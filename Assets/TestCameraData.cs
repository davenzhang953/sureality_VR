using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TestCameraData : MonoBehaviour
{
    public Transform xr_origin;
    public Transform mainCamera;
    public Transform seenge_root;
    public Text text;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = $"xr_origin wpos:{xr_origin.transform.position} lpos:{xr_origin.transform.localPosition}, mainCamera wpos:{mainCamera.transform.position}, mainCamera lpos:{mainCamera.transform.localPosition},seenge_root:{seenge_root.transform.position} , seenge_root:lpos:{seenge_root.transform.localPosition}";
    }
}
