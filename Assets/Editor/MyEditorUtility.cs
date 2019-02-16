using System;
using UnityEditor;

public static class MyEditorUtility
{
    public static void ProgressBar(string title, Action<IProgress<(string info, float progress)>> action)
    {
        try
        {
            var progress = new ProgressBarProgress(title);
            action(progress);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private class ProgressBarProgress : IProgress<(string info, float progress)>
    {
        private readonly string _title = null;
        public ProgressBarProgress(string title)
        {
            _title = title;
        }

        public void Report((string info, float progress) value)
        {
            EditorUtility.DisplayProgressBar(_title, value.info, value.progress);
        }
    }
}
