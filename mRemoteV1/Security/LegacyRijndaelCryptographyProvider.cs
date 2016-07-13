using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using mRemoteNG.App;
using mRemoteNG.Messages;


namespace mRemoteNG.Security
{
	public class LegacyRijndaelCryptographyProvider : ICryptographyProvider
	{
        public int BlockSizeInBytes { get; }

        public string CipherEngine { get; }

	    public LegacyRijndaelCryptographyProvider()
	    {
	        CipherEngine = "Rijndael";
	        BlockSizeInBytes = 16;
	    }


        public string Encrypt(string strToEncrypt, SecureString strSecret)
		{
			if (strToEncrypt == "" || strSecret.Length == 0)
				return strToEncrypt;
			
			try
			{
				var rd = new RijndaelManaged();

                var md5 = new MD5CryptoServiceProvider();
                var key = md5.ComputeHash(Encoding.UTF8.GetBytes(strSecret.ConvertToUnsecureString()));
					
				md5.Clear();
				rd.Key = key;
				rd.GenerateIV();

                var iv = rd.IV;
                var ms = new MemoryStream();
					
				ms.Write(iv, 0, iv.Length);

                var cs = new CryptoStream(ms, rd.CreateEncryptor(), CryptoStreamMode.Write);
                var data = Encoding.UTF8.GetBytes(strToEncrypt);
					
				cs.Write(data, 0, data.Length);
				cs.FlushFinalBlock();

                var encdata = ms.ToArray();
				cs.Close();
				rd.Clear();
					
				return Convert.ToBase64String(encdata);
			}
			catch (Exception ex)
			{
				Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(Language.strErrorEncryptionFailed, ex.Message));
			}
				
			return strToEncrypt;
		}
			
		public string Decrypt(string ciphertextBase64, SecureString password)
		{
			if (string.IsNullOrEmpty(ciphertextBase64) || password.Length == 0)
				return ciphertextBase64;
				
			try
			{
                var plaintext = "";

                using (var rijndaelManaged = new RijndaelManaged())
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var key = md5.ComputeHash(Encoding.UTF8.GetBytes(password.ConvertToUnsecureString()));
                    rijndaelManaged.Key = key;
                    var ciphertext = Convert.FromBase64String(ciphertextBase64);

                    using (var memoryStream = new MemoryStream(ciphertext))
                    using (var cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8, true))
                    {
                        var iv = new byte[BlockSizeInBytes];
                        memoryStream.Read(iv, 0, BlockSizeInBytes);
                        rijndaelManaged.IV = iv;
                        plaintext = streamReader.ReadToEnd();
                        rijndaelManaged.Clear();
                    }
                }

			    return plaintext;
			}
			catch (Exception ex)
			{
				// Ignore CryptographicException "Padding is invalid and cannot be removed." when password is incorrect.
				if (!(ex is CryptographicException))
				{
					Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(Language.strErrorDecryptionFailed, ex.Message));
				}
			}
				
			return ciphertextBase64;
		}
	}
}