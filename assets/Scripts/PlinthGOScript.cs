using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;

[System.Serializable]
[ExecuteInEditMode]
public class PlinthGOScript : MonoBehaviour {

    Ellipsoid e = new Ellipsoid();

    [ExecuteInEditMode]
    [SerializeField]
    private float _Latitude;
    public float Latitude
    {
        
        get { return _Latitude; }
        [ExecuteInEditMode]
        set {
            _Latitude = value;
            DVec3 v = e.ToVector(Ellipsoid.ToRad(_Longitude), Ellipsoid.ToRad(_Latitude), 0);
            Debug.Log("in latitude " + v);
            gameObject.transform.position = new Vector3((float)v.x,(float)v.y,(float)v.z);
        }
    }

    [SerializeField]
    private float _Longitude;
    public float Longitude
    {
        get { return _Longitude; }
        set {
            _Longitude = value;
            DVec3 v = e.ToVector(Ellipsoid.ToRad(_Longitude), Ellipsoid.ToRad(_Latitude), 0);
            Debug.Log("in longitude " + v);
            gameObject.transform.position = new Vector3((float)v.x, (float)v.y, (float)v.z);
        }
    }

    // Use this for initialization
    void Start () {
        //Debug.Log("in start...");
        //DVec3 v = e.ToVector(Ellipsoid.ToRad(_Longitude), Ellipsoid.ToRad(_Latitude), 0);
        //gameObject.transform.position = new Vector3((float)v.x, (float)v.y, (float)v.z);
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
