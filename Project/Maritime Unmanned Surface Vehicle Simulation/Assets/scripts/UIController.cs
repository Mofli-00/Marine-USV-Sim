using UnityEngine;
using UnityEngine.UI;

// 类名必须与文件名 UIController.cs 完全一致
public class UIController : MonoBehaviour
{
    [Header("UI 元素")]
    // 在 Unity Inspector 面板中拖入你的 Start Button
    public GameObject startButton;

    [Header("控制对象")]
    // 在 Unity Inspector 面板中拖入挂载了 BoatControlSimple 的无人艇
    public BoatControlSimple boatScript;

    void Start()
    {
        // 游戏启动时，确保无人艇处于静止不可控状态
        if (boatScript != null)
        {
            boatScript.enabled = false;
        }
        else
        {
            Debug.LogWarning("UIController: 未找到关联的 BoatControlSimple 脚本！");
        }
    }

    /// <summary>
    /// 当点击按钮时触发的函数
    /// </summary>
    public void OnStartButtonClick()
    {
        // 1. 使按钮消失
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        // 2. 激活 BoatControlSimple 脚本，从而启用 WASD 控制
        if (boatScript != null)
        {
            boatScript.enabled = true;
            Debug.Log("仿真已开始：现在可以使用 WASD 控制无人艇。");
        }
    }
}