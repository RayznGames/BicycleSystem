using UnityEngine;

public class ContactProvider : MonoBehaviour
{
    bool contact;  
    public bool GetCOntact() 
    {
        return contact;
    }
	private void OnTriggerStay(Collider other)
	{
		contact = true;
	}
	private void OnTriggerExit(Collider other)
	{
		contact = false;
	}
}
