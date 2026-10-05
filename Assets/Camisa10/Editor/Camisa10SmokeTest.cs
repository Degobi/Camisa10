using System.Collections.Generic;
using Camisa10.Core;
using Camisa10.UI;
using UnityEditor;
using UnityEngine;

namespace Camisa10.EditorTools
{
    /// <summary>
    /// Teste automático dos lances 3D: entra em Play, joga cada tipo de lance (segura e solta CHUTAR, toca nos botões)
    /// e conta erros no Console. Linha de comando:
    /// Unity -batchmode -projectPath . -executeMethod Camisa10.EditorTools.Camisa10SmokeTest.Run -logFile -
    /// </summary>
    [InitializeOnLoad]
    public static class Camisa10SmokeTest
    {
        const string Flag = "Camisa10.SmokeTest";

        // entrar em Play recarrega os scripts: o teste se reconecta aqui
        static Camisa10SmokeTest()
        {
            if (!SessionState.GetBool(Flag, false)) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }

        static readonly string[] Types = { "penalti", "falta", "chance", "cabeceio", "corte", "cruzamento", "rebote", "cara", "contra", "meio", "defesa", "gol" }; // "gol": lance de chance encerrado como gol (comemoração completa)
        static int index, errors;
        static float stepAt;
        static int stage;
        static ChanceHud hud;
        static Chance3D chance;
        static bool done, skillTried, celebrated;
        static readonly List<string> report = new List<string>();
        static GameObject canvasGo;

        [MenuItem("Camisa 10/Testar lances automaticamente")]
        public static void Run()
        {
            SessionState.SetBool(Flag, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (stack != null && stack.Contains("UnityEditor.Search")) return; // falha interna do índice de busca do editor
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                report.Add($"ERRO em '{(index < Types.Length ? Types[index] : "?")}': {msg}\n{stack}");
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (canvasGo == null)
            {
                // tira o jogo normal do caminho e monta só o lance
                var app = Object.FindAnyObjectByType<GameApp>();
                if (app != null) app.gameObject.SetActive(false);
                canvasGo = new GameObject("CanvasTeste", typeof(RectTransform));
                var c = canvasGo.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                StartNext();
                return;
            }
            float t = Time.time - stepAt;
            if (Types[index] == "gol")
            {
                if (stage == 0 && t > 1.6f) { chance.TestGoal(); stage = 2; stepAt = Time.time; return; }
                if (stage == 2 && !done && hud.SkipBtn.gameObject.activeSelf && !celebrated) { celebrated = true; report.Add("    gol: comemoração começou"); }
                if (stage == 2 && (done || t > 16f))
                {
                    if (!done) { errors++; report.Add("ERRO em 'gol': a comemoração não terminou em 16 s"); }
                    if (!celebrated) { errors++; report.Add("ERRO em 'gol': a comemoração não apareceu"); }
                    if (chance != null) Object.Destroy(chance.gameObject);
                    hud?.Destroy();
                    Finish();
                }
                return;
            }
            switch (stage)
            {
                case 0 when t > 1.6f && System.Array.IndexOf(new[] { "chance", "cara", "contra", "meio", "cruzamento", "rebote" }, Types[index]) >= 0 && !skillTried:
                    // com a bola nos pés: tenta uma firula antes de chutar
                    skillTried = true;
                    hud.Skill.OnPress?.Invoke();
                    stepAt = Time.time + 1f;
                    break;
                case 0 when t > 1.6f:
                    // depois da apresentação: segura o chute (ou desarma, ou cabeceia na hora)
                    if (Types[index] == "defesa") hud.TackleL.OnPress?.Invoke();
                    else if (Types[index] == "chance" || Types[index] == "cara") hud.Finesse.OnPress?.Invoke(); // chute colocado
                    else if (Types[index] != "cabeceio" && Types[index] != "corte") hud.Shoot.OnPress?.Invoke();
                    stage = 1; stepAt = Time.time; break;
                case 1 when (Types[index] == "cabeceio" || Types[index] == "corte") && t > .1f:
                    hud.Shoot.OnPress?.Invoke(); // tenta cabecear quando a janela abrir
                    if (done || t > 3f) { stage = 2; stepAt = Time.time; }
                    break;
                case 1 when Types[index] != "cabeceio" && Types[index] != "corte" && t > .7f:
                    hud.Shoot.OnRelease?.Invoke();
                    stage = 2; stepAt = Time.time; break;
                case 2 when !done && hud.SkipBtn.gameObject.activeSelf:
                    // gol: a comemoração do pênalti roda inteira; nas outras o teste aperta PULAR
                    if (!celebrated) { celebrated = true; report.Add($"    {Types[index]}: comemoração começou"); }
                    if (Types[index] != "penalti" && Time.time - stepAt > 4f) hud.SkipBtn.OnPress?.Invoke();
                    break;
                case 2 when done || t > 16f:
                    if (!done) { errors++; report.Add($"ERRO em '{Types[index]}': o lance não terminou em 16 s"); }
                    if (chance != null) Object.Destroy(chance.gameObject);
                    hud?.Destroy();
                    index++;
                    if (index >= Types.Length) { Finish(); return; }
                    StartNext();
                    break;
            }
        }

        static void StartNext()
        {
            var game = Game.NewCareer("Teste", "ATA", new[] { 4, 4, 3, 3, 3, 3 });
            var match = new MatchEngine(game) { Current = new Moment { Type = Types[index] == "gol" ? "chance" : Types[index], Text = "Teste automático", Options = new MomentOption[0] } };
            done = false; stage = 0; stepAt = Time.time; skillTried = false; celebrated = false;
            hud = ChanceHud.Build(canvasGo.transform);
            string type = Types[index];
            chance = Chance3D.Play(null, game, match, hud, o => { done = true; report.Add($"ok  {type,-9} → {o}"); });
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Flag, false);
            Debug.Log("[Camisa 10] Teste dos lances:\n" + string.Join("\n", report) + $"\n{errors} erro(s).");
            if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
            else EditorApplication.ExitPlaymode();
        }
    }
}
