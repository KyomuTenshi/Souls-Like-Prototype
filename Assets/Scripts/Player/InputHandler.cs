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

        PlayerControls inputActions;

        Vector2 movementInput;
        Vector2 cameraInput;

        private void Update()
        {
            float delta = Time.deltaTime;
            TickInput(delta);
        }

        public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();

                // Параметр лямбды переименован из "inputActions" в "ctx": он затенял
                // одноимённое поле класса, что путало при чтении (и рискованно при
                // будущих правках — легко случайно обратиться не к тому "inputActions").
                inputActions.PlayerMovement.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Movement.canceled += ctx => movementInput = Vector2.zero;

                inputActions.PlayerMovement.Camera.performed += ctx => cameraInput = ctx.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.canceled += ctx => cameraInput = Vector2.zero;

                // rollFlag больше не выставляется тут по событию нажатия — иначе
                // Roll срабатывал бы мгновенно при любом нажатии Shift, включая
                // начало удержания для спринта, и персонаж всегда перекатывался
                // бы вместо того чтобы побежать. Теперь rollFlag/sprintFlag
                // считает HandleRollInput() по длительности удержания кнопки
                // (короткое нажатие -> Roll, удержание -> Sprint), см. TickInput().
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

        // Короткое нажатие Shift -> Roll, удержание дольше 0.5с -> Sprint.
        // Раньше тут проверялась inputActions.PlayerActions.Roll.phase ==
        // InputActionPhase.Started, но у Button-действия без interactions фаза
        // почти сразу переходит в Performed, поэтому Started не годится для
        // отслеживания "кнопка всё ещё удерживается" каждый кадр. IsPressed()
        // как раз для этого и предназначен — надёжно отражает текущее состояние.
        private void HandleRollInput(float delta)
        {
            b_Input = inputActions.PlayerActions.Roll.IsPressed();

            if (b_Input)
            {
                rollInputTimer += delta;
                sprintFlag = true;
            } else
            {
                if(rollInputTimer > 0 && rollInputTimer < 0.5f)
                {
                    sprintFlag = false;
                    rollFlag = true;
                }

                rollInputTimer = 0;
            }
        }
    }
}