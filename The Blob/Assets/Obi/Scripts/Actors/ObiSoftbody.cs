using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obi{

	[ExecuteInEditMode]
	[RequireComponent(typeof (ObiPinConstraints))]
	[RequireComponent(typeof (ObiShapeMatchingConstraints))]
	[DisallowMultipleComponent]
	public class ObiSoftbody : ObiActor {

		[Header("Particle generation")]

		public Mesh inputMesh;
		[Tooltip("Radius of particles. Use small values to create a finer representation of the mesh (max 1 particle per vertex), and large values to create a rough representation.")]
		public float particleRadius = 0.1f;			/**< Radius of particles.*/
		[Range(0,0.75f)]
		[Tooltip("Percentage of overlap allowed between particles.")]
		public float particleOverlap = 0.2f;		/**< Percentage of overlap allowed for particles. A value of zero will not allow particles to overlap. Using values between 0.2 and 0.5 is recommended for good surface coverage.*/

		[Range(0,1)]
		[Tooltip("Amount of shape smoothing applied before generating particles.")]
		public float shapeSmoothing = 0.5f;

		[Tooltip("Radius around each particle used to calculate their anisotropy.")]
		public float anisotropyNeighborhood = 0.2f;	/**< Neighborhood around each particle. Used to calculate their anisotropy (size and orientation).*/

		[Tooltip("Maximum aspect ratio allowed for particles. High values will allow particles to deform more to better fit their neighborhood.")]
		public float maxAnisotropy = 3;				/**< Maximum particle anisotropy. High values will allow particles to deform to better fit their neighborhood.*/

		[Tooltip("Size of shape matching clusters. Large radii will include more particles in each cluster, making the softbody more rigid and increasing computational cost. If parts of the softbody are detaching, increase the radius so that they are included in at least one cluster.")]
		public float softClusterRadius = 0.3f;		/**< Size of clusters. Particles belonging to the same cluster are linked together by a shape matching constraint.*/

		[Tooltip("Generates one-sided particles instead of round ones. This results in better penetration recovery for convex objects.")]
		public bool oneSided = false;

		[SerializeField][HideInInspector] private int centerShape = -1;

		public ObiPinConstraints PinConstraints{
			get{return GetConstraints(Oni.ConstraintType.Pin) as ObiPinConstraints;}
		}
		public ObiShapeMatchingConstraints ShapeMatchingConstraints{
			get{return GetConstraints(Oni.ConstraintType.ShapeMatching) as ObiShapeMatchingConstraints;}
		}

		public override bool AddToSolver(object info){
			
			if (Initialized && base.AddToSolver(info)){
				RecalculateCenterShape();
				return true;
			}
			return false;
		}

		public override void OnSolverStepEnd(float deltaTime){

			ObiShapeMatchingConstraintBatch batch = ShapeMatchingConstraints.GetFirstBatch();
			IList<int> shapes = batch.ActiveConstraints;
			
			if (Application.isPlaying && isActiveAndEnabled && centerShape > -1 && centerShape < shapes.Count){ 

				int shape = shapes[centerShape];
	
				transform.position = (Vector3)batch.coms[shape] - batch.orientations[shape] * batch.restComs[shape]; 
				transform.rotation = batch.orientations[shape];

			}

		}

		/**
		 * Recalculates the shape used as reference for transform position/orientation when there are no fixed particles.
		 * Should be called manually when changing the amount of fixed particles and/or active particles.
		 */
		public void RecalculateCenterShape(){
		
			centerShape = -1; 

			for (int i = 0; i < invMasses.Length; ++i){
				if (invMasses[i] <= 0)
					return;
			}			

			ObiShapeMatchingConstraintBatch batch = ShapeMatchingConstraints.GetFirstBatch();
			IList<int> activeShapes = batch.ActiveConstraints;
	
			float minDistance = float.MaxValue;
			for (int i = 0; i < activeShapes.Count; ++i){

				float dist = positions[batch.GetParticleIndex(activeShapes[i])].sqrMagnitude;

				if (dist < minDistance){
					minDistance = dist;
					centerShape = i;
				}
			}

		}

		public override void UpdateParticlePhases(){
		
			if (!InSolver) return;
	
			for(int i = 0; i < phases.Length; i++){
				phases[i] = Oni.MakePhase(Oni.GetGroupFromPhase(phases[i]),(selfCollisions?Oni.ParticlePhase.SelfCollide:0) | (oneSided?Oni.ParticlePhase.OneSided:0));
			}
			PushDataToSolver(ParticleData.PHASES);
		}

		public override void PushDataToSolver(ParticleData data = ParticleData.NONE){

			if (!InSolver) 
				return;

			base.PushDataToSolver(data);

			// Recalculate rest shape matching data when changing particle masses.
			if ((data & ParticleData.INV_MASSES) != 0)
				foreach (ObiShapeMatchingConstraintBatch batch in ShapeMatchingConstraints.GetBatches())
					Oni.CalculateRestShapeMatching(solver.OniSolver,batch.OniBatch);
		}

		protected override IEnumerator Initialize()
		{		
			initialized = false;			
			initializing = false;
	
			RemoveFromSolver(null);
			
			if (inputMesh == null){
				Debug.LogError("No input mesh provided. Cannot initialize physical representation.");
				yield break;
			}

			initializing = true;

			initialScaleMatrix.SetTRS(Vector3.zero,Quaternion.identity,transform.lossyScale);

			Vector3[] vertices = inputMesh.vertices;
			Vector3[] normals = inputMesh.normals;
			List<Vector3> particles = new List<Vector3>();

			// Add particles to every vertex, as long as they are not too close to the already added ones:
			for (int i = 0; i < vertices.Length; ++i){

				bool intersects = false;
				Vector3 vertexScaled = initialScaleMatrix * vertices[i];
	
				for (int j = 0; j < particles.Count; ++j){
					if (Vector3.Distance(vertexScaled,particles[j]) < particleRadius * 2 * (1-particleOverlap)){
						intersects = true;
						break;
					}
				}
				if (intersects) continue;

				particles.Add(vertexScaled);
			}

			active = new bool[particles.Count];
			positions = new Vector3[particles.Count];
			orientations = new Quaternion[particles.Count];
			restPositions = new Vector4[particles.Count];
			restOrientations = new Quaternion[particles.Count];
			velocities = new Vector3[particles.Count];
			angularVelocities = new Vector3[particles.Count];
			invMasses  = new float[particles.Count];
			invRotationalMasses  = new float[particles.Count];
			principalRadii = new Vector3[particles.Count];
			phases = new int[particles.Count];

			for (int i = 0; i < particles.Count; ++i){

				// Perform ellipsoid fitting:
				Vector3 avgNormal = Vector3.zero;
				List<Vector3> neighbourVertices = new List<Vector3>();

				for (int j = 0; j < vertices.Length; ++j){

					Vector3 vertexScaled = initialScaleMatrix * vertices[j];

					if (Vector3.Distance(vertexScaled,particles[i]) < anisotropyNeighborhood){
						neighbourVertices.Add(vertexScaled);
						avgNormal += normals[j];
					}
				}
				if (neighbourVertices.Count > 0)
					avgNormal /= neighbourVertices.Count;

				Vector3 centroid = particles[i];
				Quaternion orientation = Quaternion.identity;
				Vector3 principalValues = Vector3.one;
				Oni.GetPointCloudAnisotropy(neighbourVertices.ToArray(),neighbourVertices.Count,maxAnisotropy,particleRadius,ref avgNormal,ref centroid, ref orientation,ref principalValues);

				active[i] = true;
				invRotationalMasses[i] = invMasses[i] = 1.0f;
				positions[i] = Vector3.Lerp(particles[i],centroid,shapeSmoothing);
				restPositions[i] = positions[i];
				orientations[i] = orientation;
				restOrientations[i] = orientation;
				restPositions[i][3] = 1; // activate rest position.
				principalRadii[i] = principalValues;
				phases[i] = Oni.MakePhase(1,(selfCollisions?Oni.ParticlePhase.SelfCollide:0) | (oneSided?Oni.ParticlePhase.OneSided:0));
			}

			//Create shape matching clusters:
			ShapeMatchingConstraints.Clear();

			ObiShapeMatchingConstraintBatch shapeBatch = new ObiShapeMatchingConstraintBatch(false,false);
			ShapeMatchingConstraints.AddBatch(shapeBatch);

			List<int> indices = new List<int>();

			// Generate soft clusters:
			//if (makeSoftClusters){
				for (int i = 0; i < particles.Count; ++i){
		
					indices.Clear();
					indices.Add(i);
					for (int j = 0; j < particles.Count; ++j){
						if (i != j && Vector3.Distance(particles[j],particles[i]) < softClusterRadius){
							indices.Add(j);
						}
					}
	
					shapeBatch.AddConstraint(indices.ToArray(),1,0,0,false);
	
					if (i % 500 == 0)
						yield return new CoroutineJob.ProgressInfo("ObiSoftbody: generating shape matching constraints...",i/(float)particles.Count);
				}
			//}	

			//if (makeSolidCluster){
				/*indices.Clear();
				for (int i = 0; i < particles.Count; ++i)
					indices.Add(i);
				shapeBatch.AddConstraint(indices.ToArray(),1,0,0,true);*/
			//}

			// Initialize pin constraints:
			PinConstraints.Clear();
			ObiPinConstraintBatch pinBatch = new ObiPinConstraintBatch(false,false);
			PinConstraints.AddBatch(pinBatch);

			initializing = false;
			initialized = true;

		}

		/**
		 * Deactivates all fixed particles that are attached to fixed particles only, and all the constraints
		 * affecting them.
		 */
		public IEnumerator Optimize(){

			ObiShapeMatchingConstraintBatch shapeBatch = ShapeMatchingConstraints.GetFirstBatch();
	
			// Iterate over all particles and get those fixed ones that are only linked to fixed particles.
			for (int i = 0; i < shapeBatch.ConstraintCount; ++i){
	
				if (invMasses[shapeBatch.GetParticleIndex(i)] > 0) continue;
	
				active[i] = false;
				for (int j = shapeBatch.firstIndex[i]; j < shapeBatch.firstIndex[i] + shapeBatch.numIndices[i]; ++j){
					
					// If at least one neighbour particle is not fixed, then the particle we are considering for optimization should not be removed.
					if (invMasses[shapeBatch.shapeIndices[j]] > 0){
						active[i]  = true;
						break;
					}
					
				}
				
				// Deactivate all constraints involving this inactive particle:
				if (!active[i]){
	
					// for each constraint type:
					foreach (ObiBatchedConstraints constraint in constraints){
	
						// for each constraint batch (usually only one)
						if (constraint != null){
							foreach (ObiConstraintBatch batch in constraint.GetBatches()){
		
								// deactivate constraints that affect the particle:
								List<int> affectedConstraints = batch.GetConstraintsInvolvingParticle(i);
								foreach (int j in affectedConstraints) batch.DeactivateConstraint(j);
								batch.SetActiveConstraints();
							}
						}
					}
	
				}

				yield return new CoroutineJob.ProgressInfo("ObiSoftbody: optimizing constraints...",i/(float)shapeBatch.ConstraintCount);
	
			}	
	
			PushDataToSolver(ParticleData.ACTIVE_STATUS);

			// Perform skinning:
			/*IEnumerator bind = BindSkin();
			while(bind.MoveNext()){
				yield return bind.Current;
			}*/
		}
	
		/**
		 * Undoes the optimization performed by Optimize(). This means that all particles and constraints in the
		 * cloth are activated again.
		 */
		public IEnumerator Unoptimize(){
		
			// Activate all particles and constraints (particles first):
			
			for (int i = 0; i < active.Length; ++i)
			 	active[i] = true;
	
			PushDataToSolver(ParticleData.ACTIVE_STATUS);
	
			// for each constraint type:
			foreach (ObiBatchedConstraints constraint in constraints){
	
				// for each constraint batch (usually only one)
				if (constraint != null){
					foreach (ObiConstraintBatch batch in constraint.GetBatches()){
		
						// activate all constraints:
						for (int i = 0; i < batch.ConstraintCount; ++i){
							batch.ActivateConstraint(i);
							yield return new CoroutineJob.ProgressInfo("ObiSoftbody: reverting constraint optimization...",i/(float)batch.ConstraintCount);
						}

						batch.SetActiveConstraints();
					}
				}

			}

			// Perform skinning:
			/*IEnumerator bind = BindSkin();
			while(bind.MoveNext()){
				yield return bind.Current;
			}*/
		}

		/**
 		* Resets mesh to its original state.
 		*/
		public override void ResetActor(){
	
			PushDataToSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES | ParticleData.ANGULAR_VELOCITIES);
			
			if (particleIndices != null){
				for(int i = 0; i < particleIndices.Length; ++i){
					solver.renderablePositions[particleIndices[i]] = positions[i];
				}
			}

		}
	}
}
