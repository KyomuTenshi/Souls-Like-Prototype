using UnityEngine;

// Вне namespace SG — как в туториале, чтобы будущие уроки и уже настроенные
// состояния в Animator Controller продолжали находить этот behaviour.
public class ResetAnimatorBool : StateMachineBehaviour
{
    public string targetBool;
    public bool status;

    int cachedHash;
    bool validated;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Однократная проверка: существует ли bool-параметр с ТОЧНО таким
        // именем. Animator.SetBool сравнивает строку с учётом регистра, и
        // опечатка вроде 'isinteracting' вместо 'isInteracting' раньше
        // сыпала безликие варнинги "Parameter ... does not exist" каждый
        // вход в состояние. Теперь ошибка одна, конкретная и с объектом.
        if (!validated)
        {
            validated = true;

            bool exists = false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == targetBool)
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                cachedHash = Animator.StringToHash(targetBool);
            }
            else
            {
                cachedHash = 0;
                Debug.LogError(
                    $"ResetAnimatorBool: bool-параметр '{targetBool}' не найден в Animator '{animator.runtimeAnimatorController.name}'. " +
                    "Проверь регистр букв (например, должно быть 'isInteracting') в поле Target Bool на состоянии с этим behaviour.",
                    animator);
            }
        }

        if (cachedHash != 0)
        {
            animator.SetBool(cachedHash, status);
        }
    }
}