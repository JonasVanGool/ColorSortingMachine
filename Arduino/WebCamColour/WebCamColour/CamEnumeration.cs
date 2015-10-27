using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;

namespace WebCamColour
{
    public class CaptureDeviceEnumerator
    {
        public CaptureDeviceEnumerator()
        {
            AvailableVideoInputDevices = new List<DsDevice>();
            GetAvailableVideoInputDevices();
        }

        public List<DsDevice> AvailableVideoInputDevices { get; private set; }

        private void GetAvailableVideoInputDevices()
        {
            DsDevice[] videoInputDevices =
                DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            AvailableVideoInputDevices.AddRange(videoInputDevices);
        }
    }
}
