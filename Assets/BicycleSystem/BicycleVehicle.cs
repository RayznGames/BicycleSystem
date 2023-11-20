using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BicycleVehicle : MonoBehaviour
{
	float horizontalInput;
	float verticalInput;

	public Transform handle;
	bool braking;
	Rigidbody rb;

	public Vector3 COG;

	[SerializeField] float motorForce;
	[SerializeField] float brakeForce;
	float currentBrakeForce;

	float steeringAngle;
	[SerializeField] float currentSteeringAngle;
	[Range(0f, 0.1f)] [SerializeField] float speedSteerControlTime;
	[SerializeField] float maxSteeringAngle;
	[Range(0.000001f, 1)] [SerializeField] float turnSmoothing;

	[SerializeField]float maxLayingAngle = 45f;
	public float targetLayingAngle;
	[Range(-40, 40)]public float layingAmount;
	[Range(0.000001f, 1 )] [SerializeField] float leanSmoothing;

	[SerializeField] WheelCollider frontWheel;
	[SerializeField] WheelCollider backWheel;

	[SerializeField] Transform frontWheelTransform;
	[SerializeField] Transform backWheelTransform;

	[SerializeField] TrailRenderer frontTrail;
	[SerializeField] TrailRenderer rearTrail;

	// Start is called before the first frame update
void Start()
	{
		StopEmitTrail();
		rb = GetComponent<Rigidbody>();		
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		GetInput();
		HandleEngine();
		HandleSteering();
		UpdateWheels();
		UpdateHandles();
		LayOnTurn();
		DownPressureOnSpeed();
		EmitTrail();
	}

	public void GetInput()
	{
		horizontalInput = Input.GetAxis("Horizontal");
		verticalInput = Input.GetAxis("Vertical");
		braking = Input.GetKey(KeyCode.Space);
	}

	public void HandleEngine()
	{
		backWheel.motorTorque = verticalInput * motorForce;
		currentBrakeForce = braking ? brakeForce : 0f;
		//If we are not braking, ApplyBreaking applies a brakeForce of 0, so no conditional is needed
		ApplyBraking();
	}

	//Applies downforce, to keep the bike from bouncing off the ground over small bumps.
	public void DownPressureOnSpeed()
	{
		Vector3 downforce = Vector3.down; 
		float downPressure;
		if (rb.velocity.magnitude > 5)
		{
			downPressure = rb.velocity.magnitude;
			rb.AddForce(downforce * downPressure, ForceMode.Force);
		}
	}

	public void ApplyBraking()
	{
		frontWheel.brakeTorque = currentBrakeForce;
		backWheel.brakeTorque = currentBrakeForce;
	}

	//This function caps the maximum angle you can turn the front wheel.
	//It used to step up and down with speed, now it uses an exponential decay function.
	public void SpeedSteeringReductor() 
	{
		maxSteeringAngle = Mathf.LerpAngle(
			maxSteeringAngle, 
			//These two magic numbers were derived by doing a linear regression (on constants from the 20 lines that this replaced).
			Mathf.Clamp(66.614f * Mathf.Pow(0.893982f, rb.velocity.magnitude), 5, 50), 
			speedSteerControlTime
		);			
	}

	public void HandleSteering()
	{
		SpeedSteeringReductor();

		currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, maxSteeringAngle * horizontalInput, turnSmoothing);
		frontWheel.steerAngle = currentSteeringAngle;

		//We set the target laying angle to the + or - input value of our steering 
		//We invert our input for rotating in the ocrrect axis
		targetLayingAngle = maxLayingAngle * -horizontalInput;		
	}
	private void LayOnTurn()
	{
		Vector3 currentRot = transform.rotation.eulerAngles;

		//Case: not moving much
		if (rb.velocity.magnitude < 1)
		{
			layingAmount = Mathf.LerpAngle(layingAmount, 0f, 0.05f);		
			transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, layingAmount);
			return;
		}
		//Case: Not steering or steering a tiny amount
		if (currentSteeringAngle < 0.5f && currentSteeringAngle > -0.5  )
		{
			layingAmount =  Mathf.LerpAngle(layingAmount, 0f, leanSmoothing);			
		}
		//Case: Steering
		else
		{
			layingAmount = Mathf.LerpAngle(layingAmount, targetLayingAngle, leanSmoothing );		
			rb.centerOfMass = new Vector3(rb.centerOfMass.x, COG.y, rb.centerOfMass.z);
		}

		transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, layingAmount);
	}

	public void UpdateWheels()
	{
		UpdateSingleWheel(frontWheel, frontWheelTransform);
		UpdateSingleWheel(backWheel, backWheelTransform);
	}

	public void UpdateHandles()
	{		
		Quaternion sethandleRot;
		sethandleRot = frontWheelTransform.rotation;		
		handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle, handle.localEulerAngles.z);
	}

	private void EmitTrail() 
	{	
		frontTrail.emitting = frontWheel.GetGroundHit(out WheelHit Fhit);
		rearTrail.emitting = backWheel.GetGroundHit(out WheelHit Rhit);
	}

	private void StopEmitTrail() 
	{
		frontTrail.emitting = false;
		rearTrail.emitting = false;
	}

	private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
	{
		Vector3 position;
		Quaternion rotation;
		wheelCollider.GetWorldPose(out position, out rotation);
		wheelTransform.rotation = rotation;
		wheelTransform.position = position;
	}
}
