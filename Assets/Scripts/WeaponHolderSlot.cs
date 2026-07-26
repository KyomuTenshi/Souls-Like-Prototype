using UnityEngine;

namespace SG {
    public class WeaponHolderSlot : MonoBehaviour
    {
        public Transform parentOverride;
        public bool isLeftHandSlot;
        public bool isRightHandSlot;

        public GameObject currentWeaponModel;

        public void UnloadWeapon()
        {
            if (currentWeaponModel != null)
            {
                currentWeaponModel.SetActive(false);
            }
        }

        public void UnloadWeaponAndDestroy()
        {
            if (currentWeaponModel != null)
            {
                Destroy(currentWeaponModel);
                // БЫЛО: после Destroy() ссылка currentWeaponModel не обнулялась
                // явно. Unity's Destroy() уничтожает объект не мгновенно, а в
                // конце кадра, и до этого момента (и даже пару обращений после,
                // если полагаться только на перегруженный ==) ссылка технически
                // "жива" — из-за этого в Inspector или при повторном быстром вызове
                // LoadWeaponModel в тот же кадр можно было словить обращение к
                // уже помеченному на удаление объекту. Обнуляем ссылку сразу и
                // явно, а не полагаемся на fake-null поведение UnityEngine.Object.
                currentWeaponModel = null;
            }
        }

        public void LoadWeaponModel(WeaponItem weaponItem)
        {
            UnloadWeaponAndDestroy();

            if (weaponItem == null || weaponItem.modelPrefab == null)
            {
                // Добавлена проверка weaponItem.modelPrefab == null — если в
                // ScriptableObject оружия забыли назначить modelPrefab,
                // Instantiate(null) кидает ArgumentException и ломает загрузку
                // остальных слотов (например, если оба — left/right — грузятся
                // подряд в PlayerInventory.Start()). Теперь просто разгружаем
                // слот, не крашась.
                UnloadWeapon();
                return;
            }

            GameObject model = Instantiate(weaponItem.modelPrefab) as GameObject;
            if (model != null)
            {
                if (parentOverride != null)
                {
                    model.transform.parent = parentOverride;
                }
                else
                {
                    model.transform.parent = transform;
                }

                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
            }

            currentWeaponModel = model;
        }
    }
}