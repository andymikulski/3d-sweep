using UnityEngine;
using System.Collections;
 
[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class MouseOrbitZoom : MonoBehaviour {
 
    public Transform target;
    public float distance = 8.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
 
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
 
    public float distanceMin = 8f;
    public float distanceMax = 15f;

	public float smoothSpeed = 0.1f;
 
    private float x = 0.0f;
    private float y = 0.0f;
 
    // Use this for initialization
    void Start () 
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void Update () 
    {
		if (target == null)
			return;

		Quaternion rotation = Quaternion.identity;

		if (Input.GetKey (KeyCode.LeftShift) && Input.GetMouseButton (0)) {
			Cursor.lockState = CursorLockMode.Confined;
			x += Input.GetAxis ("Mouse X") * xSpeed * 0.02f;
			y -= Input.GetAxis ("Mouse Y") * ySpeed * 0.02f;
		} else {
			Cursor.lockState = CursorLockMode.None;
		}

		y = ClampAngle (y, yMinLimit, yMaxLimit);
		rotation = Quaternion.Euler (y, x, 0);

		distance = Mathf.Clamp (distance - Input.GetAxis ("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

		Vector3 negDistance = new Vector3 (0.0f, 0.0f, -distance);
		Vector3 position = rotation * negDistance + target.position;
	
		transform.rotation = Quaternion.Lerp (transform.rotation, rotation, Time.deltaTime * smoothSpeed);
		transform.position = Vector3.Lerp (transform.position, position, Time.deltaTime * smoothSpeed);
    }
 
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
