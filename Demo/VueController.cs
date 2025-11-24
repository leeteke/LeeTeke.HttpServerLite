using LeeTeke.HttpServerLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo
{
    internal class VueController:VueHistoryModeRouter
    {

        public VueController() : base("/vue/")
        {
        }
    }
}
