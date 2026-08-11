using System.Collections;
using UnityEngine;

namespace SG {
    // Хитстоп: микро-заморозка времени в момент попадания — главный "вес"
    // удара в NieR/DMC. Реализован через Time.timeScale: единственный способ
    // заморозить всё сразу (анимации, физику, камеру) без ссылок на
    // участников удара. Носитель создаёт себя сам при первом Play().
    //
    // ВАЖНО: если появится пауза или slow-mo через Time.timeScale (witch
    // time), класс нужно научить восстанавливать чужое значение timeScale,
    // а не жёсткую единицу.
    public class HitStop : MonoBehaviour
    {
        static HitStop instance;

        Coroutine running;

        // duration — в реальных секундах (unscaled). slowScale = 0.05, а не
        // полный 0: лёгкое "доползание" кадра выглядит мягче стоп-кадра, и
        // деление на deltaTime гарантированно не ловит ноль.
        public static void Play(float duration, float slowScale = 0.05f)
        {
            if (duration <= 0f)
                return;

            if (instance == null)
            {
                GameObject go = new GameObject("HitStop (auto)");
                instance = go.AddComponent<HitStop>();
                DontDestroyOnLoad(go);
            }

            instance.PlayInternal(duration, slowScale);
        }

        void PlayInternal(float duration, float slowScale)
        {
            // Новый удар перезапускает таймер, а не складывает длительности —
            // серия попаданий не превращает игру в слайд-шоу.
            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(DoHitStop(duration, slowScale));
        }

        IEnumerator DoHitStop(float duration, float slowScale)
        {
            Time.timeScale = slowScale;

            // Realtime: обычный WaitForSeconds замедлился бы вместе с timeScale.
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            running = null;
        }

        void OnDestroy()
        {
            // Смена сцены посреди заморозки не должна оставить время замедленным.
            if (instance == this)
            {
                Time.timeScale = 1f;
                instance = null;
            }
        }
    }
}