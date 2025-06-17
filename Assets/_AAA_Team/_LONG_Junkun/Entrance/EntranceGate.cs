using UnityEngine;

public class EntranceGate : MonoBehaviour
{
    public GameObject entranceManager;  // 引用 EntranceManager
    public GameObject canvasXDK;        // 引用 CanvasXDK

    private void Start()
    {
        // 确保开始时 EntranceManager 和 CanvasXDK 状态正确
        if (entranceManager != null)
        {
            entranceManager.SetActive(true);
        }
        if (canvasXDK != null)
        {
            canvasXDK.SetActive(false);
        }
    }

    // 当 MainCamera 与 EntranceGate 发生碰撞时
    private void OnCollisionEnter(Collision collision)
    {
        // 检查碰撞对象是否是 MainCamera
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            Debug.Log("Enter");
            // 关闭 EntranceManager
            if (entranceManager != null)
            {
                entranceManager.SetActive(false);
            }

            // 开启 CanvasXDK
            if (canvasXDK != null)
            {
                canvasXDK.SetActive(true);
            }
        }
    }

    // 如果使用的是触发器 (Trigger)，则改用 OnTriggerEnter
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            Debug.Log("Enter");
            // 关闭 EntranceManager
            if (entranceManager != null)
            {
                entranceManager.SetActive(false);
            }

            // 开启 CanvasXDK
            if (canvasXDK != null)
            {
                canvasXDK.SetActive(true);
            }
        }
    }
}
