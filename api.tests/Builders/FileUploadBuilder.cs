using System;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Http;

namespace api.tests.Builders;

public class FileUploadBuilder
{
    private FileUpload _fileUpload;

    public FileUploadBuilder() {
        _fileUpload = new FileUpload();
    }

    public FileUploadBuilder WithId(string Id) {
        _fileUpload.Id = Id;

        return this;
    }

    public FileUploadBuilder WithOriginalUrl(string OriginalUrl) {
        _fileUpload.OriginalUrl = OriginalUrl;

        return this;
    }

    public FileUploadBuilder WithCreatedAt(DateTime CreatedAt) {
        _fileUpload.CreatedAt = CreatedAt;

        return this;
    }

    public FileUploadBuilder WithFile(IFormFile File) {
        _fileUpload.File = File;

        return this;
    }

    public FileUploadBuilder WithNote(string Note) {
        _fileUpload.Note = Note;

        return this;
    }

    public FileUploadBuilder WithExpiryDuration(ExpiryDuration ExpiryDuration) {
        _fileUpload.ExpiryDuration = ExpiryDuration;

        return this;
    }

    public FileUpload Build() {
        return new FileUpload() {
            Id = _fileUpload.Id,
            OriginalUrl = _fileUpload.OriginalUrl,
            CreatedAt = _fileUpload.CreatedAt,
            File = _fileUpload.File,
            Note = _fileUpload.Note,
            ExpiryDuration = _fileUpload.ExpiryDuration
        };
    }

    public static IEnumerable<FileUpload> BuildMany(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            yield return new FileUploadBuilder()
                .WithId($"{Guid.NewGuid()} {i}")
                .WithFile(new FormFile(Stream.Null, 0, 0, "file", "file.txt"))
                .WithExpiryDuration(ExpiryDuration.OneMinute)
                .Build();
        }
    }


}
