using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideBlacklist : MonoBehaviour
{
    public string blacklistTag;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter (Collision collision) {

        if (collision.gameObject.CompareTag(blacklistTag)) {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
        }

    }
}
