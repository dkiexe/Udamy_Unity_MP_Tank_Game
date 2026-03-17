using UnityEngine;

public class Projectile : MonoBehaviour
{

    public int TeamID { get; private set; }

    public void Initialise(int TeamID)
    {
        this.TeamID = TeamID;
    }
}
