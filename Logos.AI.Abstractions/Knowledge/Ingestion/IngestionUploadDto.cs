using System.ComponentModel;
using Logos.AI.Abstractions.Common;
// ReSharper disable MemberCanBePrivate.Global
namespace Logos.AI.Abstractions.Knowledge.Ingestion;

/// <summary>
/// Представляє дані, що використовуються для завантаження файлів у систему індексації.
/// </summary>
[Description("Дані для завантаження файлів у систему індексації")]
public record IngestionUploadDto
{
	private readonly byte[] _fileData = [];
	public Guid DocumentId { get; private set; }
	public string FileName { get; private set; } = string.Empty;
	public string Title { get; private set; } = string.Empty;
	public string Description { get; private set; } = string.Empty;
	public string FileExtension { get; private set; } = string.Empty;
	public byte[] FileData
	{
		get => _fileData;
		private init
		{
			_fileData = value;
			DocumentId = GuidUtils.GenerateGuidFromSeed(_fileData);
			FileExtension = FileSignatureUtils.GetExtensionFromBytes(_fileData);
		}
	}
	public IngestionUploadDto(string path) : this(File.ReadAllBytes(path), Path.GetFileName(path))
	{
		
	}
	public IngestionUploadDto(byte[] fileData, string fileName)
	{
		try
		{
			FileData = fileData;
			FileName = fileName;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
	public IngestionUploadDto(string base64Content, string fileName) : this(Convert.FromBase64String(base64Content), fileName)
	{
	}
	public IngestionUploadDto SetDescription(string description)
	{
		Description = description;
		return this;
	}
	public IngestionUploadDto SetTitle(string title)
	{
		Title = title;
		return this;
	}
	public IngestionUploadDto SetFileName(string fileName)
	{
		FileName = fileName;
		return this;
	}
}
