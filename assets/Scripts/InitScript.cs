using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Assets.Scripts;


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
        Ellipsoid e = new Ellipsoid();
//        double sx = 0, sy = 0, sz = 0;
        foreach (GnomeObject creature in Creatures)
        {
            DVec3 v = e.ToVector(creature.lon*3.14/180.0, creature.lat*3.14/180.0, 0);
            Debug.Log(creature.lon + " "+ creature.lat + " " + v.x + " " + v.y + " " + v.z);
//           sx += v.x; sy += v.y; sz += v.z;
        }
//       int n = Creatures.Length;
//       Debug.Log("Centre: " + (sx / n) + " " + (sy / n) + " " + (sz / n));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
