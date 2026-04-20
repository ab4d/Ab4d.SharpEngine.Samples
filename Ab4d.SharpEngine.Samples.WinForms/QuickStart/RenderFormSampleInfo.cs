using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ab4d.SharpEngine.Samples.WinForms.QuickStart
{
    public partial class RenderFormSampleInfo : UserControl
    {
        public RenderFormSampleInfo()
        {
            InitializeComponent();
        }

        // Starting RenderFormSample from this Form would run at half the frame rate
        // because two message pumps are running (one for this Form and one for RenderFormSample).
        // What is more, it is not possible to close the RenderFormSample window from this form
        // because it creates a re-entrant DoEvents() situation that prevents the close sequence from completing.
        //
        // private void button1_Click(object sender, EventArgs e)
        // {
        //     using (var game = new RenderFormSample(this.FindForm()))
        //         game.Run();
        // }
    }
}
