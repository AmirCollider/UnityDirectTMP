// ==========================================
// DirectTMPHealthCheckWindow
// The health report, drawn.
//
// Deliberately plain: a banner that says whether
// anything is wrong, one line per check, a fix button
// beside the checks that have one, and a Copy button
// at the bottom. Nobody opens this window for pleasure
// - they open it because something did not work - so
// the shortest path from "open" to "understand" wins
// over anything decorative.
// ==========================================
using UnityEditor;
using UnityEngine;

namespace UnityDirectTMP.Editor
{
    internal sealed class DirectTMPHealthCheckWindow : EditorWindow
    {
        private DirectHealthReport _report;
        private Vector2 _scroll;

        public static void ShowWindow()
        {
            var window = GetWindow<DirectTMPHealthCheckWindow>(true, DirectTMPConstants.ShortName + " · Health Check", true);
            window.minSize = new Vector2(520, 460);
            window.Refresh();
            window.Show();
        }

        private void OnEnable()
        {
            if (_report == null) { Refresh(); }
        }

        private void Refresh()
        {
            _report = DirectTMPHealth.Run();
        }

        private void OnGUI()
        {
            DirectTMPBrand.Header(
                DirectTMPText.L("Health Check", "動作チェック", "بررسی سلامت"),
                DirectTMPText.L(
                    "What this package can see of your project, and anything standing in its way.",
                    "このパッケージから見えるプロジェクトの状態と、動作を妨げている要因を表示します。",
                    "چیزی که این پکیج از پروژه‌ت می‌بینه، و هر چیزی که سر راهشه."));

            if (_report == null) { Refresh(); }

            DrawBanner();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    foreach (DirectHealthEntry entry in _report.Sorted())
                    {
                        DrawEntry(entry);
                    }
                }
                GUILayout.Space(10);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawActions();
                    DirectTMPBrand.Footer();
                }
                GUILayout.Space(10);
            }
            GUILayout.Space(6);
        }

        private void DrawBanner()
        {
            DirectHealthLevel worst = _report.Worst;

            string message;
            MessageType type;

            switch (worst)
            {
                case DirectHealthLevel.Problem:
                    message = string.Format(
                        DirectTMPText.L(
                            "{0} thing(s) are stopping this package from working properly.",
                            "{0} 件、正常な動作を妨げている問題があります。",
                            "{0} مورد جلوی کارِ درست این پکیج رو گرفته."),
                        _report.CountAt(DirectHealthLevel.Problem));
                    type = MessageType.Error;
                    break;

                case DirectHealthLevel.Warning:
                    message = string.Format(
                        DirectTMPText.L(
                            "Everything works. {0} thing(s) are worth a look.",
                            "動作に問題はありません。{0} 件、確認をおすすめします。",
                            "همه‌چیز کار می‌کنه. {0} مورد ارزش نگاه کردن داره."),
                        _report.CountAt(DirectHealthLevel.Warning));
                    type = MessageType.Warning;
                    break;

                default:
                    message = DirectTMPText.L(
                        "Everything checks out.",
                        "すべて正常です。",
                        "همه‌چیز درسته.");
                    type = MessageType.Info;
                    break;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(message, type);
                GUILayout.Space(10);
            }
            GUILayout.Space(4);
        }

        private void DrawEntry(DirectHealthEntry entry)
        {
            using (new EditorGUILayout.VerticalScope(DirectTMPBrand.Card))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color previous = GUI.contentColor;
                    GUI.contentColor = ColorFor(entry.Level);
                    GUILayout.Label(DirectTMPHealth.Marker(entry.Level), GUILayout.Width(30));
                    GUI.contentColor = previous;

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(entry.Subject, EditorStyles.boldLabel);
                        GUILayout.Label(entry.Finding, EditorStyles.wordWrappedLabel);

                        if (!string.IsNullOrEmpty(entry.Advice))
                        {
                            GUILayout.Label(entry.Advice, DirectTMPBrand.Subtitle);
                        }
                    }

                    if (entry.Fix != null && !string.IsNullOrEmpty(entry.ActionLabel))
                    {
                        if (GUILayout.Button(entry.ActionLabel, EditorStyles.miniButton, GUILayout.Width(170), GUILayout.Height(22)))
                        {
                            entry.Fix();
                            Refresh();
                        }
                    }
                }
            }
        }

        private static Color ColorFor(DirectHealthLevel level)
        {
            switch (level)
            {
                case DirectHealthLevel.Problem: return DirectTMPBrand.Blush;
                case DirectHealthLevel.Warning: return DirectTMPBrand.Warn;
                default: return DirectTMPBrand.Ok;
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(DirectTMPText.Word("refresh"), GUILayout.Height(26)))
                {
                    Refresh();
                }

                if (GUILayout.Button(
                        DirectTMPText.L("Copy report", "レポートをコピー", "کپی گزارش"),
                        GUILayout.Height(26)))
                {
                    EditorGUIUtility.systemCopyBuffer = DirectTMPHealth.Format(_report);
                    ShowNotification(new GUIContent(
                        DirectTMPText.L("Copied", "コピーしました", "کپی شد")));
                }

                if (GUILayout.Button(
                        DirectTMPText.L("Report an issue", "不具合を報告", "گزارش مشکل"),
                        GUILayout.Height(26)))
                {
                    // Copy first: the issue form is more useful with the
                    // report already on the clipboard, and nobody comes back
                    // for it once the browser has opened.
                    EditorGUIUtility.systemCopyBuffer = DirectTMPHealth.Format(_report);
                    Application.OpenURL(DirectTMPConstants.IssuesUrl);
                }
            }
        }
    }
}
