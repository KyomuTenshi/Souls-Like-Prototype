using System.Collections;
using UnityEngine;

namespace SG {
    // НОВЫЙ ФАЙЛ (Фаза 2 — game feel). Хитстоп: микро-заморозка времени в
    // момент попадания удара. Это главный "вес" удара в NieR/DMC/Bayonetta:
    // на несколько сотых секунды игра почти останавливается, и мозг читает
    // это как физический контакт клинка с целью.
    //
    // Реализация — через Time.timeScale, потому что это единственный способ
    // заморозить ВСЁ сразу (анимации обеих сторон, физику, камеру) без
    // ссылок на участников удара. Никакой настройки сцены не нужно:
    // объект-носитель создаёт себя сам при первом вызове HitStop.Play().
    //
    // ВАЖНО на будущее: если в проекте появится пауза или slow-mo через
    // Time.timeScale (например, witch time), этот класс нужно будет научить
    // восстанавливать ЧУЖОЕ значение timeScale, а не жёсткую единицу.
    public class HitStop : MonoBehaviour
    {
        static HitStop instance;

        Coroutine running;

        // duration — длительность заморозки в РЕАЛЬНЫХ секундах (unscaled).
        // slowScale — во что проседает timeScale: 0.05 (а не полный 0) —
        // лёгкое "доползание" кадра выглядит мягче стоп-кадра, и код,
        // делящий на deltaTime, гарантированно не ловит ноль.
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
            // Новый удар во время заморозки не СКЛАДЫВАЕТ длительности, а
            // перезапускает таймер — серия быстрых попаданий не превращает
            // игру в слайд-шоу.
            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(DoHitStop(duration, slowScale));
        }

        IEnumerator DoHitStop(float duration, float slowScale)
        {
            Time.timeScale = slowScale;

            // Realtime: обычный WaitForSeconds сам замедлился бы вместе с
            // timeScale, и заморозка длилась бы в 20 раз дольше задуманного.
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            running = null;
        }

        void OnDestroy()
        {
            // Страховка: если носитель уничтожили посреди заморозки
            // (смена сцены), время не должно остаться замедленным.
            if (instance == this)
            {
                Time.timeScale = 1f;
                instance = null;
            }
        }
    }
}