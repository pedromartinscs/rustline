using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Integral logical-pixel layout, shared by drawing, pointer hit tests and tests.</summary>
    public sealed class FrontendLayout
    {
        public RectInt Panel { get; }
        public RectInt[] Controls { get; }
        public RectInt[] Sliders { get; }
        public int TextScale { get; }
        public int TitleScale { get; }
        public int HeaderY { get; }
        public int FooterY { get; }
        public bool Compact { get; }

        public FrontendLayout(int width, int height, FrontendState state)
        {
            bool title = state == FrontendState.Title;
            bool options = state == FrontendState.TitleOptions || state == FrontendState.PauseOptions;
            bool credits = state == FrontendState.Credits;
            Compact = width < 520 || height < 430;
            int margin = width < 200 || height < 200 ? 4 : 16;
            int panelWidth = Mathf.Min(title ? 600 : 560, Mathf.Max(1, width - margin * 2));
            int panelHeight = Mathf.Min(title ? 430 : credits ? 440 : options ? 380 : 352,
                Mathf.Max(1, height - margin * 2));
            Panel = new RectInt((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
            TextScale = panelWidth >= 440 && panelHeight >= 300 ? 2 : 1;
            TitleScale = Mathf.Max(1, Mathf.Min(title ? 9 : 5,
                Mathf.Min((panelWidth - 32) / 48, panelHeight / (title ? 52 : 60))));
            HeaderY = Panel.y + (Compact ? 12 : 30);
            FooterY = Panel.yMax - (Compact ? 13 : 24);
            int count = title ? 4 : credits ? 1 : 3;
            if (state == FrontendState.Playing || state == FrontendState.Transition) count = 0;
            Controls = new RectInt[count];
            Sliders = new RectInt[options ? 2 : 0];
            int inset = Compact ? 12 : 38;
            int top = HeaderY + TitleScale * 7 + (Compact ? 18 : 44);
            int bottom = FooterY - (Compact ? 8 : 24);
            if (credits) top = bottom - (Compact ? 18 : 38);
            int spacing = Compact ? 5 : 10;
            int rowHeight = count == 0 ? 1 : Mathf.Max(12, Mathf.Min(options ? 68 : 44,
                (bottom - top - (count - 1) * spacing) / count));
            for (int i = 0; i < count; i++)
            {
                Controls[i] = new RectInt(Panel.x + inset, top + i * (rowHeight + spacing),
                    Mathf.Max(1, panelWidth - inset * 2), rowHeight);
                if (options && i < 2)
                {
                    RectInt control = Controls[i];
                    Sliders[i] = new RectInt(control.x + 8, control.yMax - (Compact ? 6 : 14),
                        Mathf.Max(1, control.width - 16), Compact ? 3 : 6);
                }
            }
        }
    }
}
