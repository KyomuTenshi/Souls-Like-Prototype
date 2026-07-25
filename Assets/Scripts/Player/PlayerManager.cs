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

        // Эти сбросы безопасны: rollFlag и sprintFlag теперь пересчитываются
        // заново каждый кадр внутри InputHandler.TickInput() -> HandleRollInput()
        // (по текущему состоянию кнопки), причём TickInput вызывается прямо в
        // начале PlayerLocomotion.Update(), непосредственно перед тем, как эти
        // флаги читаются. Поэтому не важно, в каком порядке Unity вызовет Update()
        // этого скрипта относительно PlayerLocomotion — свежее значение всё
        // равно будет выставлено до чтения.
        inputHandler.rollFlag = false;
        inputHandler.sprintFlag = false;
    }
}
}