using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isPlayerInCollider = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Space"))
        {
            isPlayerInCollider=true;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && isPlayerInCollider == true && Lasso.isInHandKey == true)
        {
            Debug.Log('1');
        }
    }
}
