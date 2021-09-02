using System;
using System.IO;
using System.Security.Cryptography;

namespace CawkVMUnpacker.Unpacking {
	internal static class CawkVMEncryption {
		internal static byte[] SeededRandomKeyXOR(byte[] input) {
			var rand = new Random(23546654);
			byte[] result = new byte[input.Length];
			for (int i = 0; i < input.Length; i++) {
				result[i] = (byte)(input[i] ^ rand.Next(0, 250));
			}

			return result;
		}

		/// <summary>
		/// Performs the decryption that would usually happen in the native runtime component.
		/// </summary>
		internal static byte[] DecryptNative(byte[] data, byte[] key) {
			int N1 = 12;
			int N2 = 14;
			int NS = 258;

			for (int i = 0; i < key.Length; i++)
				NS += NS % (key[i] + 1);

			byte[] result = new byte[data.Length];

			for (int i = 0; i < data.Length; i++) {
				NS = key[i % key.Length] + NS;
				N1 = (NS + 5) * (N1 & 0xFF) + (N1 >> 8);
				N2 = (NS + 7) * (N2 & 0xFF) + (N2 >> 8);
				NS = (N1 << 8) + N2 & 0xFF;

				result[i] = (byte)(data[i] ^ NS);
			}

			const string hcpKey = "HCP";
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(result[i] ^ hcpKey[i % hcpKey.Length]);

			return result;
		}

		internal static byte[] DecryptRijndael(byte[] data, byte[] key) {
			byte[] result;
			using (var rijndael = new RijndaelManaged()) {
				rijndael.Key = rijndael.IV = key;
				using (var stream = new MemoryStream())
				using (var decrypt = new CryptoStream(stream, rijndael.CreateDecryptor(), CryptoStreamMode.Write)) {
					decrypt.Write(data, 0, data.Length);
					decrypt.FlushFinalBlock();
					result = stream.ToArray();
				}
			}

			return result;
		}
	}
}
