/*
 * File: Line.cs
 * Author: Tom Bisch
 * Date: October 22, 2015
 * Description: This class represents a line using X and Y coordinates. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SETMystify {
    class Line {

        // instance members
        private int x1;
        private int y1;
        private int x2;
        private int y2;

        // constructor
        public Line(int pX1, int pY1, int pX2, int pY2) {
            x1 = pX1;
            y1 = pY1;
            x2 = pX2;
            y2 = pY2;
        }

        /* 
         * Function: Length
         * Parameters: None
         * Returns: The length of the line object
         * Description: This method uses the pythagorean theorem to calculate the length of the line.
         */
        public int Length() {
            return (int)Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        }

        // accessors and mutators
        public int X1 {
            get { return x1; }
            set { x1 = value; }
        }
        public int Y1 {
            get { return y1; }
            set { y1 = value; }
        }
        public int X2 {
            get { return x2; }
            set { x2 = value; }
        }
        public int Y2 {
            get { return y2; }
            set { y2 = value; }
        }
    }
}
