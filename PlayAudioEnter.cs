using UnityEngine;

public class PlayAudioEnter : StateMachineBehaviour
{
    [SerializeField] private SoundType sound;
    [SerializeField] private float volume = 1;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        AudioManager.PlaySound(sound, volume);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is create
}
