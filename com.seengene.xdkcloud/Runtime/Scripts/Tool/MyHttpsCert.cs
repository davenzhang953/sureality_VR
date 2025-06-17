using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Seengene.XDK
{
    public class MyHttpsCert : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) {
            Debug.Log("Simply return true no matter what");
            return true;
        }
    }
}
