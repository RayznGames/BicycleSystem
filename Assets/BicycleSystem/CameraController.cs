using UnityEngine;

public class CameraController : MonoBehaviour
{
	public Transform target;
	[Space(10)]
	public float sensitivity;

	Transform camPos;
	Vector3 camOffset;

	[SerializeField] float keepAtDist_Col = 0.75f;
	[SerializeField]LayerMask collidesWith;	
	
	private void Start()
	{		
		camPos = transform.GetChild(0);
		camOffset += camPos.localPosition;
		camPos.localPosition = camOffset;
	}
	// Update is called once per frame
	void Update()
	{
		transform.position = target.position;
		ThirdPersonRotate();
		CheckCamCollisionOffset();
		ZoomCamera();		
	}

	void ZoomCamera()
	{
		//Vector2 MouseScrollValue = Input.mouseScrollDelta;		
		camOffset.z += Input.mouseScrollDelta.y * 10 * Time.fixedDeltaTime;
	}

	private void ThirdPersonRotate()
	{
		// Get mouse input
		float mouseX = Input.GetAxis("Mouse X"); //moves the Y Rotation
		float mouseY = Input.GetAxis("Mouse Y");//moves the X Rotation

		// Rotate the camera horizontally (Y rotation ) based on Mouse X movement 
		transform.Rotate(Vector3.up * mouseX * sensitivity);

		float currentXRotation = transform.eulerAngles.x;

		// Limit the vertical X rotation (MouseY) to avoid camera flipping
		if (currentXRotation > 180f)
		{
			currentXRotation -= 360f;
		} 
		// Rotate the camera vertically based on mouse movement		
		float clampedXRotation = Mathf.Clamp(currentXRotation - mouseY * sensitivity, -70f, 70f);

		// Apply the rotation
		transform.rotation = Quaternion.Euler(clampedXRotation, transform.eulerAngles.y, transform.rotation.z);
	}
	private void CheckCamCollisionOffset()
	{
		//Vector3 worldPos = transform.position;
		RaycastHit hit;
		// Does the ray intersect any objects excluding the Unselected layers
		if (Physics.Raycast(transform.position, camPos.TransformDirection(Vector3.back), out hit, Mathf.Abs(camOffset.z) + keepAtDist_Col, collidesWith))
		{
			Vector3 colCorrectedPos = hit.point;
			camPos.position = colCorrectedPos - camPos.TransformDirection(Vector3.back) * (keepAtDist_Col); //minus 0.5 in the direction of the camera, this is why we add 0.5 in the cast before.
			Debug.DrawRay(transform.position, camPos.TransformDirection(Vector3.back) * hit.distance, Color.red);			
		}
		else
		{
			camPos.localPosition = camOffset;
			Debug.DrawRay(transform.position, camPos.TransformDirection(Vector3.back) * 100, Color.blue);
		}
	}

}
