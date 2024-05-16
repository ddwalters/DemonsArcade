using UnityEngine;

public class StartMenu : MonoBehaviour
{
    public transition transition;
    public GameObject GameManager;

    private void Start()
    {
        transition = GameManager.GetComponent<transition>();
    }

    public void onClick()
    {
        transition.ChangeScene();
    }
}
