using UnityEngine;

public class TimerTest : MonoBehaviour {
    [SerializeField] private float _duration = 1f;
    private float _timer = 0f;
 
    private void FixedUpdate() {
        _timer += Time.fixedDeltaTime;
        if (_timer >= _duration) {
            _timer = 0f;
            // Do Stuff here
            Debug.LogError("SSSSSSSSSSSSSSSSSSSSSS");
        }
    }
}