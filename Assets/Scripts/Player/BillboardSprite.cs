using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    [Tooltip("ON: sprite matches camera rotation exactly (flat on screen). OFF: stays upright, only turns around Y.")]
    public bool matchCameraFully = false;

    Transform cam;

    void Start() => cam = Camera.main.transform;

    void LateUpdate()
    {
        transform.rotation = matchCameraFully
            ? cam.rotation
            : Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
    }
}
