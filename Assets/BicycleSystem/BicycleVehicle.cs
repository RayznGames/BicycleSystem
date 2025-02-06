using UnityEngine;
using UnityEditor;
using System.Drawing;

public class BicycleVehicle : MonoBehaviour
{
	//debugInfo	
	float horizontalInput;
	float verticalInput;
	bool braking;
	Rigidbody rb;	

	[Header("Power/Braking")]
	[Space(5)]
	[SerializeField] float motorForce;
	[SerializeField] float brakeForce;
	public Vector3 COG;

	[Space(20)]
	[Header("Steering")]
	[Space(5)]
	[Tooltip("Defines the maximum steering angle for the bicycle")]
	[SerializeField] float maxSteeringAngle;
	[Tooltip("Sets how current_MaxSteering is reduced based on the speed of the RB, (0 - No effect) (1 - Full)")]
	[Range(0f, 1f)] [SerializeField] float steerReductorAmmount;
	[Tooltip("Sets the Steering sensitivity [Steering Stiffness] 0 - No turn, 1 - FastTurn)")]
	[Range(0.001f, 1f)] [SerializeField] float turnSmoothing;

	[Space(20)]
	[Header("Lean")]
	[Space(5)]
	[Tooltip("Defines the maximum leaning angle for this bicycle")]
	[SerializeField]float maxLeanAngle = 45f;
	[Tooltip("Sets the Leaning sensitivity (0 - None, 1 - full")]
	[Range(0.001f, 1f )] [SerializeField] float leanSmoothing;
	float targetLeanAngle;

	[Space(20)]
	[Header("Object References")]	
	public Transform handle;
	[Space(10)]
	[SerializeField] WheelCollider frontWheel;
	[SerializeField] WheelCollider backWheel;
	[Space(10)]
	[SerializeField] Transform frontWheelTransform;
	[SerializeField] Transform backWheelTransform;
	[Space(10)]
	[SerializeField] TrailRenderer frontTrail;
	[SerializeField] TrailRenderer rearTrail;
	ContactProvider frontContact;
	ContactProvider rearContact;	

	[Space(20)]
	[HeaderAttribute("Info")]
	[SerializeField] float currentSteeringAngle;
	[Tooltip("Dynamic steering angle baed on the speed of the RB, affected by sterReductorAmmount")]
	[SerializeField] float current_maxSteeringAngle;
	[Tooltip("The current lean angle applied")]
	[Range(-45, 45)]public float currentLeanAngle;
	[Space(20)]
	[HeaderAttribute("Speed")]
	[SerializeField] float currentSpeed;

	// Start is called before the first frame update
	void Start()
	{
		frontContact = frontTrail.transform.GetChild(0).GetComponent<ContactProvider>();
		rearContact = rearTrail.transform.GetChild(0).GetComponent<ContactProvider>();		
		//Important to stop bycicle from Jittering
		frontWheel.ConfigureVehicleSubsteps(5, 12, 15);
		backWheel.ConfigureVehicleSubsteps(5, 12, 15);
		rb = GetComponent<Rigidbody>();		
	}
	private void Update()
	{
		GetInput();		
	}
	// Update is called once per frame
	void FixedUpdate()
	{
		HandleEngine();
		HandleSteering();
		LeanOnTurn();
		UpdateHandles();
		UpdateWheels();		
		EmitTrail();
		Speed_O_Meter();
		//DebugInfo();
	}

	private void GetInput()
	{
		horizontalInput = Input.GetAxis("Horizontal");
		verticalInput = Input.GetAxis("Vertical");
		braking = Input.GetKey(KeyCode.Space);
	}

	private void HandleEngine()
	{		
		backWheel.motorTorque = braking? 0f : verticalInput * motorForce;	
		//If we are braking, ApplyBreaking applies brakeForce conditional is embeded in parameter	
		float force = braking ? brakeForce : 0f;
		ApplyBraking(force);
	}
	public void ApplyBraking(float brakeForce)
	{
		frontWheel.brakeTorque = brakeForce;
		backWheel.brakeTorque = brakeForce;	
	}

	//This replaces the (Magic numbers) that controlled an exponential decay function for maxteeringAngle (maxSteering angle was not adjustable)
	//This one alows to customize Default bike maxSteeringAngle parameters and maxSpeed allowing for better scalability for each vehicle	
	/// <summary>
	/// Reduces the current maximum Steering based on the speed of the Rigidbody multiplied by SteerReductionAmmount (0-1)  
	/// </summary>
	void MaxSteeringReductor() 
	{
		//30 is the value of MaxSpeed at wich currentMaxSteering will be at its minimum,			
		float t = (rb.linearVelocity.magnitude / 30)  * steerReductorAmmount;		
		t = t > 1? 1 : t; 
		current_maxSteeringAngle = Mathf.LerpAngle(maxSteeringAngle, 5, t);	//5 is the lowest posisble degrees of Steering	
	}
	
	public void HandleSteering()
	{		
		MaxSteeringReductor();

		currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, current_maxSteeringAngle * horizontalInput, turnSmoothing * 0.1f);
		frontWheel.steerAngle = currentSteeringAngle;

		//We set the target lean angle to the + or - input value of our steering 
		//We invert our input for rotating in the ocrrect axis
		targetLeanAngle = maxLeanAngle * -horizontalInput;		
	}
	public void UpdateHandles()
	{		
		handle.localEulerAngles = new Vector3(handle.localEulerAngles.x, currentSteeringAngle, handle.localEulerAngles.z);
		//handle.Rotate(Vector3.up, currentSteeringAngle, Space.Self);		
	}
	private void LeanOnTurn()
	{
		Vector3 currentRot = transform.rotation.eulerAngles;
		//Case: not moving much		
		if (rb.linearVelocity.magnitude < 1)
		{
			currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, 0f, 0.1f);		
			transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
			//return;
		}
		//Case: Not steering or steering a tiny amount
		if (currentSteeringAngle < 0.5f && currentSteeringAngle > -0.5  )
		{
			currentLeanAngle =  Mathf.LerpAngle(currentLeanAngle, 0f, leanSmoothing * 0.1f);			
		}
		//Case: Steering
		else
		{
			currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, leanSmoothing * 0.1f );		
			rb.centerOfMass = new Vector3(rb.centerOfMass.x, COG.y, rb.centerOfMass.z);
		}
		transform.rotation = Quaternion.Euler(currentRot.x, currentRot.y, currentLeanAngle);
	}
	public void UpdateWheels()
	{
		UpdateSingleWheel(frontWheel, frontWheelTransform);
		UpdateSingleWheel(backWheel, backWheelTransform);
	}
	private void EmitTrail() 
	{
		if (braking)
		{
			frontTrail.emitting = frontContact.GetCOntact();
			rearTrail.emitting = rearContact.GetCOntact();
		}
		else
		{			
			frontTrail.emitting = false;
			rearTrail.emitting = false;
		}		
	}

	void DebugInfo() 
	{
		frontWheel.GetGroundHit(out WheelHit frontInfo);
		backWheel.GetGroundHit(out WheelHit backInfo);

		float backCoefficient = (backInfo.sidewaysSlip / backWheel.sidewaysFriction.extremumSlip);
		float frontCoefficient = (frontInfo.sidewaysSlip / frontWheel.sidewaysFriction.extremumSlip);

		//Debug.Log(" Back Coeficient = " + backCoefficient );
		//Debug.Log(" Front Coeficient = " + frontCoefficient);	
	}	

	private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
	{
		Vector3 position;
		Quaternion rotation;
		wheelCollider.GetWorldPose(out position, out rotation);
		wheelTransform.rotation = rotation;
		wheelTransform.position = position;
	}	

	void Speed_O_Meter() 
	{
		currentSpeed = rb.linearVelocity.magnitude;
	}
}

#region CustomInspector
[CustomEditor(typeof(BicycleVehicle))]
//We need to extend the Editor
public class BicycleInspector :  Editor
{
	//Here we grab a reference to our component
	BicycleVehicle bicycle;

	private void OnEnable()
	{
		//target is by default available for you in Editor		
		bicycle = target as BicycleVehicle;
	}

	//Here is the meat of the script
	public override void OnInspectorGUI()
	{
		SetLabel("Bicycle System", 30, FontStyle.Bold, TextAnchor.UpperLeft);		
		SetLabel("Love from RayznGames", 12, FontStyle.Italic, TextAnchor.UpperRight);		
		base.OnInspectorGUI();		
	}
	void SetLabel(string title, int size, FontStyle style,TextAnchor alignment) 
	{
		GUI.skin.label.alignment = alignment;
		GUI.skin.label.fontSize = size;
		GUI.skin.label.fontStyle = FontStyle.Bold;
		GUILayout.Label(title);
	}
}
/*
		GUI.skin.label.alignment = TextAnchor.UpperRight;
		GUI.skin.label.fontSize = 12;
		GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
		GUILayout.Label("Love from RayznGames");

		EditorGUILayout.PrefixLabel("Text");
		myScript.someString = EditorGUILayout.TextField(myScript.someString);
		EditorGUILayout.PrefixLabel("Number");
		myScript.someNumber = EditorGUILayout.IntSlider(myScript.someNumber, 0, 10);
		EditorGUI.indentLevel--;
 */

#endregion
