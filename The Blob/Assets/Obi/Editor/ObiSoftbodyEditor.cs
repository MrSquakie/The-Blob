using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiRope components.
	 * Allows particle selection and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiSoftbody)), CanEditMultipleObjects] 
	public class ObiSoftbodyEditor : ObiParticleActorEditor
	{

		public class SoftbodyParticleProperty : ParticleProperty
		{
		  public const int RotationalMass = 3;

		  public SoftbodyParticleProperty (int value) : base (value){}
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Softbody (fully set up)",false,4)]
		static void CreateObiSoftbody()
		{
			GameObject c = new GameObject("Obi Softbody");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Softbody");
			ObiSoftbody body = c.AddComponent<ObiSoftbody>();
			ObiSoftbodySkinner skinner = c.AddComponent<ObiSoftbodySkinner>();
			body.Solver = c.AddComponent<ObiSolver>();
		}
		
		ObiSoftbody body;
		
		public override void OnEnable(){
			base.OnEnable();
			body = (ObiSoftbody)target;

			particlePropertyNames.AddRange(new string[]{"Rotational Mass"});
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < body.positions.Length; i++)
			{
				wsPositions[i] = body.GetParticlePosition(i);
				wsOrientations[i] = body.GetParticleOrientation(i);	
				facingCamera[i] = true;		
			}

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			if (index >= 0 && index < body.invMasses.Length){
				switch(property){
					case SoftbodyParticleProperty.Mass: 
							body.invMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break; 
					case SoftbodyParticleProperty.RotationalMass: 
							body.invRotationalMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break; 
					case SoftbodyParticleProperty.Radius:
							body.principalRadii[index] = Vector3.one * value;
						break;
					case SoftbodyParticleProperty.Layer:
							body.phases[index] = Oni.MakePhase((int)value,(body.SelfCollisions?Oni.ParticlePhase.SelfCollide:0) | (body.oneSided?Oni.ParticlePhase.OneSided:0));
						break;
				}
			}
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			if (index >= 0 && index < body.invMasses.Length){
				switch(property){
					case SoftbodyParticleProperty.Mass:
						return 1.0f/body.invMasses[index];
					case SoftbodyParticleProperty.RotationalMass:
						return 1.0f/body.invRotationalMasses[index];
					case SoftbodyParticleProperty.Radius:
						return body.principalRadii[index][0];
					case SoftbodyParticleProperty.Layer:
						return Oni.GetGroupFromPhase(body.phases[index]);
				}
			}
			return 0;
		}

		protected override void UpdatePropertyInSolver(){

			base.UpdatePropertyInSolver();

			switch(currentProperty){
				case SoftbodyParticleProperty.RotationalMass:
				 	body.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES);
				break;
			}

		}

		protected override void FixSelectedParticles(){
			base.FixSelectedParticles();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (body.invRotationalMasses[i] != 0){	
						SetPropertyValue(SoftbodyParticleProperty.RotationalMass,i,Mathf.Infinity);
						newProperty = GetPropertyValue(currentProperty,i);
						body.angularVelocities[i] = Vector3.zero;
					}
				}
			}
			body.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES | ParticleData.ANGULAR_VELOCITIES);
		}

		protected override void FixSelectedParticlesTranslation(){
			base.FixSelectedParticlesTranslation();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (body.invRotationalMasses[i] == 0){	
						SetPropertyValue(SoftbodyParticleProperty.RotationalMass,i,1);
						newProperty = GetPropertyValue(currentProperty,i);
					}
				}
			}
			body.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES | ParticleData.ANGULAR_VELOCITIES);
		}

		protected override void UnfixSelectedParticles(){
			base.UnfixSelectedParticles();
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i]){
					if (body.invRotationalMasses[i] == 0){	
						SetPropertyValue(SoftbodyParticleProperty.RotationalMass,i,1);
						newProperty = GetPropertyValue(currentProperty,i);
					}
				}
			}
			body.PushDataToSolver(ParticleData.INV_ROTATIONAL_MASSES);
		}	

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			GUI.enabled = body.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",Resources.Load<Texture2D>("EditParticles")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}
			GUI.enabled = true;			

			EditorGUILayout.LabelField("Status: "+ (body.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = body.inputMesh != null;
			if (GUILayout.Button("Initialize")){
				if (!body.Initialized){
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					routine = job.Start(body.GeneratePhysicRepresentationForMesh());
					EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
					EditorGUIUtility.ExitGUI();
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
						CoroutineJob job = new CoroutineJob();
						routine = job.Start(body.GeneratePhysicRepresentationForMesh());
						EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
						EditorGUIUtility.ExitGUI();
					}
				}
			}
			GUI.enabled = true;

			if (body.inputMesh == null){
				EditorGUILayout.HelpBox("No input mesh present.",MessageType.Info);
			}
			/*if (body.target == null || body.target.sharedMesh == null){
				EditorGUILayout.HelpBox("No target mesh present.",MessageType.Info);
			}*/

			GUI.enabled = body.Initialized;
			if (GUILayout.Button("Set Rest State")){
				Undo.RecordObject(body, "Set rest state");
				body.PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Optimize")){
				Undo.RecordObject(body, "Optimize");
				CoroutineJob job = new CoroutineJob();
				routine = job.Start(body.Optimize());
				EditorCoroutine.ShowCoroutineProgressBar("Optimizing...",ref routine);
				EditorGUIUtility.ExitGUI();
			}
			if (GUILayout.Button("Unoptimize")){
				Undo.RecordObject(body, "Unoptimize");
				CoroutineJob job = new CoroutineJob();
				routine = job.Start(body.Unoptimize());
				EditorCoroutine.ShowCoroutineProgressBar("Reverting optimization...",ref routine);
				EditorGUIUtility.ExitGUI();
			}
			GUILayout.EndHorizontal();
			GUI.enabled = true;	

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
	}
}


