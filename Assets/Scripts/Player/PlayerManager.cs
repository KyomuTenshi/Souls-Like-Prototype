using UnityEngine;

namespace SG {
public class PlayerManager : MonoBehaviour
{

    InputHandler inputHandler;
    Animator anim;

    void Start()
    {
        inputHandler = GetComponent<InputHandler>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        inputHandler.isIntetacting = anim.GetBool("isInteracting");

        // rollFlag больше не сбрасывается тут — это делает сам PlayerLocomotion
        // в HandleRollingAndSprinting() сразу после того, как флаг использован.
        // Обнуление флага "вслепую" каждый кадр здесь могло произойти раньше,
        // чем PlayerLocomotion успевал его прочитать (порядок Update() между
        // скриптами не гарантирован Unity), из-за чего Roll не срабатывал.
    }
}
}