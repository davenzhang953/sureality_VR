using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scaning : MonoBehaviour
{
    public float DelayToHide = 2f;

    public string AnimName = "Scan";

    [SerializeField]
    private GameObject objWorking;

    [SerializeField]
    private GameObject objSuccess;

    public void StartWork()
    {
        var animator = GetComponent<Animator>();
        if (animator)
        {
            animator.Play(AnimName);
        }
        objWorking.SetActive(true);
        objSuccess.SetActive(false);
    }

    public void OnSuccess()
    {
        objWorking.SetActive(false);
        objSuccess.SetActive(true);
        StartCoroutine(delayToDo());
    }

    private IEnumerator delayToDo()
    {
        yield return new WaitForSeconds(DelayToHide);
        gameObject.SetActive(false);
    }
}
