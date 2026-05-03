using UnityEngine;

public class Blink_Animation : MonoBehaviour
{
    [SerializeField] private Material openMat;
    [SerializeField] private Material blinkMat;

    private Renderer _renderer;
    private float _timer;
    private bool _isBlinking;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (!_isBlinking && _timer >= 5f)
        {
            _isBlinking = true;
            _timer = 0f;
            _renderer.sharedMaterial = blinkMat;
        }
        else if (_isBlinking && _timer >= 0.2f)
        {
            _isBlinking = false;
            _timer = 0f;
            _renderer.sharedMaterial = openMat;
        }
    }
}
