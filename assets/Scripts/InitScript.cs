using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ellipsoid
//6378137.0 6378137.0 6356752.314245

public enum CreatureType { Gnome, Bat, Otter, Bee };

public class GnomeObject
{
    public string Name;
    public CreatureType Species;
    public double lat, lon;
    public GnomeObject(string Name, CreatureType Species, double lat, double lon)
    {
        this.Name = Name;
        this.Species = Species;
        this.lat = lat;
        this.lon = lon;
    }
}

public class InitScript : MonoBehaviour {

    public GnomeObject[] Creatures =
    {
        new GnomeObject("wombat",CreatureType.Gnome,51.545671,-0.02399),
        new GnomeObject("yusuf",CreatureType.Gnome,51.55017,-0.01522),
        new GnomeObject("loki",CreatureType.Gnome,51.53547,-0.01331),
        new GnomeObject("parker",CreatureType.Gnome,51.54739,-0.02365),
        new GnomeObject("jetpack gnomey",CreatureType.Gnome,51.54752,-0.01052),
        new GnomeObject("super gnome",CreatureType.Gnome,51.54696,-0.01392),
        new GnomeObject("moonlight",CreatureType.Bat,51.54113,-0.01077),
        new GnomeObject("goku",CreatureType.Bat,51.5485,-0.01628),
        new GnomeObject("shadow blade",CreatureType.Bat,51.54103,-0.01384),
        new GnomeObject("zack",CreatureType.Otter,51.54022,-0.01155),
        new GnomeObject("khadija",CreatureType.Otter,51.54746,-0.015),
        new GnomeObject("denchu",CreatureType.Otter,51.54592,-0.01443),
        new GnomeObject("beehigh",CreatureType.Bee,51.54464,-0.0199),
        new GnomeObject("rosie",CreatureType.Bee,51.53878,-0.01232)
    };


    // Use this for initialization
    void Start () {
        Debug.Log("InitScript::Start");
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
