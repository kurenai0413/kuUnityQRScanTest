using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HoloToolkit.Unity;
using UnityEngine.SceneManagement;

public class ButtonBehavior : MonoBehaviour {

	public void LoadScene(int level)
    {
        SceneManager.LoadScene(level);
    }

    public void OnGazeEnter()
    {
        GetComponent<Button>().OnPointerEnter(null);
    }

    public void OnGazeLeave()
    {
        GetComponent<Button>().OnPointerExit(null);
    }
}
