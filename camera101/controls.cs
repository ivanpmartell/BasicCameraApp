using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace camera101
{
    public class thButton : Button
    {
        public thButton()
        {
            this.BackColor = color.darkFore;
            this.FlatStyle = FlatStyle.Flat;
            this.ForeColor = color.darkText;
            base.AutoSize = true;
            base.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = fixedMinSize;
            this.Padding = fixedPadding;
            this.AutoEllipsis = true;
        }

        private readonly Padding fixedPadding = new Padding(6, 0, 5, 0);
        private readonly Size fixedMinSize = new Size(75, 23);

        public override Size MinimumSize
        {
            get { return fixedMinSize; }
            set { /* ignore */ }
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.Padding = fixedPadding;
            base.OnPaddingChanged(e);
        }

    }
}
