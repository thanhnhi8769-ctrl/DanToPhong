using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform player; // Kéo xe của Nhi vào đây
    public float height = 50f; // Độ cao của camera so với xe

    void LateUpdate()
    {
        // MinimapController owns follow behavior when both legacy scripts are on this camera.
        if (GetComponent<MinimapController>() != null) return;
        if (player != null)
        {
            Vector3 newPosition = player.position;
            newPosition.y = height; // Giữ nguyên độ cao
            transform.position = newPosition;

            // Nếu muốn minimap xoay theo hướng xe thì bật dòng dưới, còn không thì bỏ qua
            // transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
    }
}
