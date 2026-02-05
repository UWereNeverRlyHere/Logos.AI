namespace Logos.AI.Abstractions.Common;
public static class FileSignatureUtils
{ 
	public static string GetExtensionFromBytes(byte[]? data)
	{
		if (data == null || data.Length < 4) return string.Empty;
		return data switch
		{
			// PDF: %PDF (25 50 44 46)
			[0x25, 0x50, 0x44, 0x46, ..] => ".pdf",
			// PNG: 89 50 4E 47 (0D 0A 1A 0A)
			[0x89, 0x50, 0x4E, 0x47, ..] => ".png",
			// JPEG: FF D8 FF
			[0xFF, 0xD8, 0xFF, ..]       => ".jpg",
			// GIF: GIF8 (47 49 46 38)
			[0x47, 0x49, 0x46, 0x38, ..] => ".gif",
			// ZIP, DOCX, XLSX, ODT: PK.. (50 4B 03 04)
			[0x50, 0x4B, 0x03, 0x04, ..] => ".docx", // Или .zip
			// Старый Office (doc, xls, ppt): D0 CF 11 E0
			[0xD0, 0xCF, 0x11, 0xE0, ..] => ".doc",
			_ => ".bin" // Default case
		};
		
	}
}