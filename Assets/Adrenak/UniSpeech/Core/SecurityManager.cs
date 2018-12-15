using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Adrenak.UniSpeech {
    public static class SecurityManager {
        public static void Init() {
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
        }

        static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            bool isOk = true;
            if (sslPolicyErrors != SslPolicyErrors.None) {
                for (int i = 0; i < chain.ChainStatus.Length; i++) {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid) {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }
    }
}