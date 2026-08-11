using UnityEngine;

// Вне namespace SG — как в туториале: уже настроенные состояния в Animator
// Controller и будущие уроки находят behaviour по этому имени.
public class ResetAnimatorBool : StateMachineBehaviour
{
    public string targetBool;
    public bool status;

    int cachedHash;
    bool validated;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // SetBool сравнивает имя с учётом регистра: опечатка в Target Bool
        // иначе сыпала бы безликий варнинг на каждый вход в состояние.
        // Проверяем один раз и ругаемся конкретно, с ссылкой на объект.
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
                    "Проверь регистр букв в поле Target Bool.",
                    animator);
            }
        }

        if (cachedHash != 0)
        {
            animator.SetBool(cachedHash, status);
        }
    }
}