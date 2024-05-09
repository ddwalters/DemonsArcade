using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string selectedScene;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "HitCone") return;
        SceneManager.LoadScene(selectedScene); // Dungeon
    }
}