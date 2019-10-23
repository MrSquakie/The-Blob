using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obi{

	[ExecuteInEditMode]
	[RequireComponent(typeof(SkinnedMeshRenderer))]
	public class ObiSoftbodySkinner : MonoBehaviour {

		[Tooltip("The ratio at which the cluster's influence on a vertex falls off with distance.")]
		public float m_skinningFalloff = 1.0f;
		[Tooltip("The maximum distance a cluster can be from a vertex before it will not influence it any more.")]
		public float m_skinningMaxDistance = 0.5f;
	
	    [HideInInspector][SerializeField] Matrix4x4[] m_bindposes = new Matrix4x4[0];
	    [HideInInspector][SerializeField] BoneWeight[] m_boneWeights = new BoneWeight[0];

		private SkinnedMeshRenderer target;
		private Mesh softMesh;					/**< deformable mesh instance*/
	 	private Transform[] bones = new Transform[0];

		[HideInInspector][SerializeField] private bool bound = false;

		[SerializeField][HideInInspector] private ObiSoftbody source;
		public ObiSoftbody Source{
			set{
	
				if (source != null){
					source.OnAddedToSolver -= Source_OnAddedToSolver;
					source.OnRemovedFromSolver -= Source_OnRemovedFromSolver;
					source.OnInitialized -= Source_OnInitialized;
				}
	
					source = value;
	
				if (source != null){
					source.OnAddedToSolver += Source_OnAddedToSolver;
					source.OnRemovedFromSolver += Source_OnRemovedFromSolver;
					source.OnInitialized += Source_OnInitialized;

					Source_OnInitialized(this,null);
				}
	
			}
			get{return source;}
		}

		void Source_OnInitialized (object sender, EventArgs e)
		{
			if (!bound){

				if (Application.isPlaying){

					IEnumerator g = BindSkin();
					while (g.MoveNext()) {}

				}else{

					// In editor, show a progress bar while binding the skin:
					CoroutineJob job = new CoroutineJob();
					IEnumerator routine = job.Start(BindSkin());
					EditorCoroutine.ShowCoroutineProgressBar("Binding to particles...",ref routine);

				}		
			}
		}
	
		public void Awake(){
			
			// autoinitialize "target" with the first skinned mesh renderer we find up our hierarchy.
			target = GetComponent<SkinnedMeshRenderer>();

			// autoinitialize "source" with the first softbody we find up our hierarchy.
			if (source == null)
				source = GetComponentInParent<ObiSoftbody>();

			Source_OnInitialized(this,null);

			CreateBones();
		}

		public void OnEnable(){
			if (source != null){
				if (source.Solver != null)
					source.Solver.OnFrameEnd += UpdateBones;
				source.OnInitialized += Source_OnInitialized;	
			}		
		}

		public void OnDisable(){
			if (source != null){
				if (source.Solver != null)
					source.Solver.OnFrameEnd -= UpdateBones;	
				source.OnInitialized -= Source_OnInitialized;
			}		
		}

		public void OnDestroy(){
			DestroyBones();
		}

		void Source_OnAddedToSolver(object sender, EventArgs e){
			source.Solver.OnFrameEnd += UpdateBones;
		}

		void Source_OnRemovedFromSolver(object sender, EventArgs e){
			source.Solver.OnFrameEnd -= UpdateBones;
		}

		private void UpdateBones(object sender, EventArgs e){

			if (bones.Length > 0 && source.InSolver)
	        {
				ObiShapeMatchingConstraintBatch batch = source.ShapeMatchingConstraints.GetFirstBatch();
				IList<int> activeShapes = batch.ActiveConstraints;
				int particleIndex;
	            for (int i = 0; i < activeShapes.Count; ++i)
	            {
					particleIndex = source.particleIndices[batch.GetParticleIndex(activeShapes[i])];
	                bones[i].position = source.Solver.renderablePositions   [particleIndex];
	                bones[i].rotation = source.Solver.renderableOrientations[particleIndex];
	            }
	        }
		}
		
		public IEnumerator BindSkin(){

			bound = false;

			if (source == null || !source.Initialized || target.sharedMesh == null){
				yield break;
			}			

			ObiShapeMatchingConstraintBatch batch = source.ShapeMatchingConstraints.GetFirstBatch();
			IList<int> activeShapes = batch.ActiveConstraints;
	
			Vector3[] clusterCenters = new Vector3[activeShapes.Count];
			Quaternion[] clusterOrientations = new Quaternion[activeShapes.Count];
	
			Vector3[] vertices = target.sharedMesh.vertices;
			m_bindposes = new Matrix4x4[activeShapes.Count];
			m_boneWeights = new BoneWeight[vertices.Length];

			// Calculate softbody local to world matrix, and target to world matrix.
			Matrix4x4 source2w = source.transform.localToWorldMatrix * source.InitialScaleMatrix.inverse;
			Matrix4x4 target2w = transform.localToWorldMatrix;
	
			// Create bind pose matrices, one per shape matching cluster:
	        for (int i = 0; i < clusterCenters.Length; ++i)
	        {
				// world space bone center/orientation:
	            clusterCenters[i] = source2w.MultiplyPoint3x4(source.restPositions[batch.GetParticleIndex(activeShapes[i])]);
				clusterOrientations[i] = source2w.rotation * source.restOrientations[batch.GetParticleIndex(activeShapes[i])];

				// world space bone transform * object local to world.
				m_bindposes[i] = Matrix4x4.TRS(clusterCenters[i], clusterOrientations[i], Vector3.one).inverse * target2w;

				yield return new CoroutineJob.ProgressInfo("ObiSoftbody: calculating bind poses...",i/(float)clusterCenters.Length);
	        }
	
			// Calculate skin weights and bone indices:
			for (int j = 0; j < m_boneWeights.Length; ++j)
			{
				// transform each vertex to world space:
				m_boneWeights[j] = CalculateBoneWeights(target2w.MultiplyPoint3x4(vertices[j]),clusterCenters);
				yield return new CoroutineJob.ProgressInfo("ObiSoftbody: calculating bone weights...",j/(float)m_boneWeights.Length);
			}

			bound = true;
		}

		private BoneWeight CalculateBoneWeights(Vector3 vertex,Vector3[] clusterCenters){

			BoneWeight w = new BoneWeight();
	
			w.boneIndex0 = -1;
			w.boneIndex1 = -1;
			w.boneIndex2 = -1;
			w.boneIndex3 = -1;

			for (int i = 0; i < clusterCenters.Length; ++i){

				float distance = Vector3.Distance(vertex,clusterCenters[i]);

				if (distance <= m_skinningMaxDistance){

					float weight = distance > 0 ? m_skinningMaxDistance/distance : 100;
					weight = Mathf.Pow(weight,m_skinningFalloff);

					if (weight > w.weight0){
						w.weight0 = weight;
						w.boneIndex0 = i;
					}else if (weight > w.weight1){
						w.weight1 = weight;
						w.boneIndex1 = i;
					}else if (weight > w.weight2){
						w.weight2 = weight;
						w.boneIndex2 = i;
					}else if (weight > w.weight3){
						w.weight3 = weight;
						w.boneIndex3 = i;
					}
				}
			}

			NormalizeBoneWeight(ref w);
			return w;
		}

		private void NormalizeBoneWeight(ref BoneWeight w){
	
			float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
			if (sum > 0){
				w.weight0 /= sum;
				w.weight1 /= sum;
				w.weight2 /= sum;
				w.weight3 /= sum;
			}
		}

		private void AppendBindposes(){
			List<Matrix4x4> bindposes = new List<Matrix4x4>(softMesh.bindposes);
            bindposes.AddRange(m_bindposes);
            softMesh.bindposes = bindposes.ToArray();
		}

		private void AppendBoneWeights(){

			int bonesOffset = softMesh.bindposes.Length;
	        BoneWeight[] boneWeights = softMesh.boneWeights.Length > 0 ? softMesh.boneWeights : new BoneWeight[m_boneWeights.Length];

	        for (int i = 0; i < boneWeights.Length; ++i)
	        {
				// If no soft skinning could be performed for this vertex, leave original skin data untouched:
	            if (m_boneWeights[i].boneIndex0 == -1 && m_boneWeights[i].boneIndex1 == -1 &&
	                m_boneWeights[i].boneIndex2 == -1 && m_boneWeights[i].boneIndex3 == -1) continue;
	
				// Copy bone weights, adding the required offset to bone indices:
	            boneWeights[i].boneIndex0 = Mathf.Max(0, m_boneWeights[i].boneIndex0) + bonesOffset;
	            boneWeights[i].weight0 = m_boneWeights[i].weight0;
	            boneWeights[i].boneIndex1 = Mathf.Max(0, m_boneWeights[i].boneIndex1) + bonesOffset;
	            boneWeights[i].weight1 = m_boneWeights[i].weight1;
	            boneWeights[i].boneIndex2 = Mathf.Max(0, m_boneWeights[i].boneIndex2) + bonesOffset;
	            boneWeights[i].weight2 = m_boneWeights[i].weight2;
	            boneWeights[i].boneIndex3 = Mathf.Max(0, m_boneWeights[i].boneIndex3) + bonesOffset;
	            boneWeights[i].weight3 = m_boneWeights[i].weight3;
	        }

	        softMesh.boneWeights = boneWeights;
		}

		private void AppendBoneTransforms(ObiShapeMatchingConstraintBatch batch){

			IList<int> activeShapes = batch.ActiveConstraints; 

			// Calculate softbody local to world matrix, and target to world matrix.
			Matrix4x4 source2w = source.transform.localToWorldMatrix * source.InitialScaleMatrix.inverse;
			Quaternion source2wRot = source2w.rotation;
				
			bones = new Transform[activeShapes.Count];
	        for (int i = 0; i < bones.Length; ++i)
	        {
	            GameObject bone = new GameObject("Cluster" + i);
	            bone.transform.parent = transform;
				bone.transform.position = source2w.MultiplyPoint3x4(source.restPositions[batch.GetParticleIndex(activeShapes[i])]);
	            bone.transform.rotation = source2wRot * source.restOrientations[batch.GetParticleIndex(activeShapes[i])];
	            bone.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
	            bones[i] = bone.transform;
	        }

	        List<Transform> originalBones = new List<Transform>(target.bones);
	        originalBones.AddRange(bones);
	        target.bones = originalBones.ToArray();
		}

		private void CopyBoneTransforms(ObiShapeMatchingConstraintBatch batch){
			IList<int> activeShapes = batch.ActiveConstraints; 
			bones = new Transform[activeShapes.Count];
			for (int i = target.bones.Length-bones.Length, j = 0; i < target.bones.Length && j < bones.Length; ++i ,++j)
				bones[j] = target.bones[i];
		}
	
		private void CreateBones(){
		
			if (Application.isPlaying)
	        {

				// Setup the mesh from scratch, in case it is not a clone of an already setup mesh.
				if (softMesh == null){

					// Create a copy of the original mesh:
					softMesh = Mesh.Instantiate(target.sharedMesh);

					// Unity bug workaround:
					Vector3[] vertices = softMesh.vertices;
					softMesh.vertices = vertices;

					// Append bone weights:
					AppendBoneWeights();
		     
					// Append bindposes:
	                AppendBindposes();
	
					AppendBoneTransforms(source.ShapeMatchingConstraints.GetFirstBatch());

				}
				// Reuse the same mesh, just copy bone references as we need to update bones every frame.
				else{
					softMesh = Mesh.Instantiate(softMesh);
		           	CopyBoneTransforms(source.ShapeMatchingConstraints.GetFirstBatch());
				}

				// Set the new mesh:
	            softMesh.RecalculateBounds();
				target.sharedMesh = softMesh;

				// Recalculate bounds:
	            target.localBounds = softMesh.bounds;
				target.rootBone = source.transform;

	        }
	
		}
	
		private void DestroyBones()
	    {
	        foreach (Transform t in bones)
	            if (t) Destroy(t.gameObject);
	
	        bones = new Transform[0];

			if (softMesh)
	            DestroyImmediate(softMesh);
	    }
	}
}
