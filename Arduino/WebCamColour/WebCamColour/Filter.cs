using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;

namespace WebCamColour
{
    [Serializable]
    public class Filter
    {
        public String _name;
        public int _minCount;
        public Bgr _minBgr;
        public Bgr _maxBgr;

        public Filter()
        {
            _name = "";
            _minCount = int.MaxValue;
            _minBgr = new Bgr(0, 0, 0);
            _maxBgr = new Bgr(255, 255, 255);
        }
        public Filter(string name, int minCount, Bgr minBgr, Bgr maxBgr)
        {
            _name = name;
            _minCount = minCount;
            _minBgr = minBgr;
            _maxBgr = maxBgr;
        }

    }
}
