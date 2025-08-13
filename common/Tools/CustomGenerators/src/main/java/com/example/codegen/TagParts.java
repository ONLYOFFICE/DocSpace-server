package com.example.codegen;

public class TagParts {
    public final String original;
    public final String folderPart;
    public final String classPart;

    public TagParts(String original, String folderPart, String classPart) {
        this.original = original;
        this.folderPart = folderPart;
        this.classPart = classPart;
    }
}