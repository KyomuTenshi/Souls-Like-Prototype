using UnityEngine;

namespace SG 
{
    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float moveAmount;
        public float mouseX;
        public float mouseY;

        public bool b_Input;

        public bool rollFlag;
        public bool sprintFlag;
        public float rollInputTimer;
        public bool isIntetacting;

        // Порог удержания, после которого поведение переключается с "Roll" на
        // "Sprint". Вынесен в поле вместо литерала "0.5f", разбросанного по коду —
        // так его видно и можно подкрутить в одном месте (в будущем — из инспектора).
        [SerializeField] private float rollInputThreshold = 0.5f;

        PlayerControls inputActions;

        Vector2 movementInput;
        Vector2 cameraInput;

        // БЫЛО: тут был собственный Update(), который тоже вызывал TickInput(delta).
        // А PlayerManager.Update() ОТДЕЛЬНО вызывает inputHandler.TickInput(delta)
        // ещё раз. В итоге TickInput() (а с ним и HandleRollInput()) отрабатывал
        // ДВАЖДЫ за один и тот же кадр. rollInputTimer при удержании кнопки
        // накручивался в два раза быстрее реального времени — порог в 0.5с
        // фактически превращался в ~0.25с. Обычное короткое нажатие переставало
        // укладываться в порог, засчитывалось как "долгое удержание", и вместо
        // Roll срабатывал Sprint. TickInput теперь вызывается ровно один раз за
        // кадр — из PlayerManager.Update(), который и остаётся единой точкой
        // тика для игрока.

        public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();

                inputActions.PlayerMovement.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += ctx => movementInput = Vector2.zero;

                inputActions.PlayerMovement.Camera.performed += ctx => cameraInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.canceled += ctx => cameraInput = Vector2.zero;

                // rollFlag намеренно НЕ выставляется тут по событию нажатия — иначе
                // Roll срабатывал бы мгновенно при любом нажатии Shift, включая
                // начало удержания для спринта. rollFlag/sprintFlag считает
                // HandleRollInput() по длительности удержания (короткое нажатие ->
                // Roll, удержание -> Sprint), см. TickInput().
            }

            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        public void TickInput(float delta)
        {
            MoveInput(delta);
            HandleRollInput(delta);
        }

        private void MoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        // Короткое нажатие Shift -> Roll, удержание дольше rollInputThreshold ->
        // Sprint. IsPressed() надёжно отражает текущее состояние кнопки каждый
        // кадр (в отличие от phase == Started, который у Button-действия без
        // interactions почти сразу переходит в Performed).
        private void HandleRollInput(float delta)
        {
            b_Input = inputActions.PlayerActions.Roll.IsPressed();

            if (b_Input)
            {
                rollInputTimer += delta;
                sprintFlag = true;
            }
            else
            {
                if (rollInputTimer > 0 && rollInputTimer < rollInputThreshold)
                {
                    sprintFlag = false;
                    rollFlag = true;
                }

                rollInputTimer = 0;
            }
        }
    }
}