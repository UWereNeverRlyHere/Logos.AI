namespace Logos.AI.Abstractions.Features.Knowledge;

public record IngestionUploadData
{
	private string _fileName = string.Empty;
	private string _title = string.Empty;
	private string _description = string.Empty;
	public string FileName { get => _fileName; set => _fileName = value; }
	public string Title { get => _title; set => _title = value; }
	public string Description { get => _description; init => _description = value; }
	public byte[] FileData { get; init; } = [];
	public IngestionUploadData(byte[] fileData, string fileName)
	{
		try
		{
			FileData = fileData;
			_fileName = fileName;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
	public IngestionUploadData(string path): this (File.ReadAllBytes(path), Path.GetFileName(path))
	{
		
	}
	public IngestionUploadData(string base64Content, string fileName): this (Convert.FromBase64String(base64Content), fileName)
	{
		
	}
	public IngestionUploadData SetDescription(string description)
	{
		_description = description;
		return this;
	}
	public IngestionUploadData SetTitle(string title)
	{
		_title = title;
		return this;
	}
	public IngestionUploadData SetFileName(string fileName)
	{
		_fileName = fileName;
		return this;
	}
};
