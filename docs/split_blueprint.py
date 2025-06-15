#!/usr/bin/env python3
"""
Script to split the SnapDog2 blueprint.md into individual chapter files.
"""

import os
import re
from pathlib import Path


def create_chapter_filename(chapter_num, title):
    """Create a filename from chapter number and title."""
    # Use the title directly (it's already clean from the chapter parsing)
    clean_title = title
    clean_title = re.sub(r'[^\w\s-]', '', clean_title)  # Remove special chars except spaces and hyphens
    clean_title = re.sub(r'\s+', '-', clean_title.strip())  # Replace spaces with hyphens
    clean_title = clean_title.lower()

    return f"{chapter_num:02d}-{clean_title}.md"

def split_blueprint():
    """Split the blueprint.md file into individual chapter files."""

    # Read the source file
    blueprint_path = Path("docs/blueprint.md")
    if not blueprint_path.exists():
        print(f"Error: {blueprint_path} not found!")
        return

    with open(blueprint_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Create output directory
    output_dir = Path("docs/blueprint")
    output_dir.mkdir(exist_ok=True)

    # Find all chapter headers (# 1, # 2, etc.)
    chapter_pattern = r'^# (\d+) (.+)$'
    chapters = []

    lines = content.split('\n')
    current_chapter = None
    current_content = []

    for i, line in enumerate(lines):
        match = re.match(chapter_pattern, line)
        if match:
            # Save previous chapter if exists
            if current_chapter:
                chapters.append({
                    'num': current_chapter['num'],
                    'title': current_chapter['title'],
                    'content': '\n'.join(current_content)
                })

            # Start new chapter
            chapter_num = int(match.group(1))
            chapter_title = match.group(2)
            current_chapter = {
                'num': chapter_num,
                'title': chapter_title
            }
            current_content = [line]  # Include the header
        else:
            if current_chapter:
                current_content.append(line)

    # Don't forget the last chapter
    if current_chapter:
        chapters.append({
            'num': current_chapter['num'],
            'title': current_chapter['title'],
            'content': '\n'.join(current_content)
        })

    print(f"Found {len(chapters)} chapters:")

    # Write each chapter to its own file
    for chapter in chapters:
        filename = create_chapter_filename(chapter['num'], chapter['title'])
        filepath = output_dir / filename

        # Clean up content - remove chapter number from header
        content_lines = chapter['content'].split('\n')
        if content_lines[0].startswith(f"# {chapter['num']} "):
            content_lines[0] = f"# {chapter['title']}"

        chapter_content = '\n'.join(content_lines)

        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(chapter_content)

        print(f"  {chapter['num']:2d}. {chapter['title']} -> {filename}")

    # Create index file
    create_index_file(chapters, output_dir)

    print(f"\nSuccessfully split blueprint into {len(chapters)} files in {output_dir}/")

def create_index_file(chapters, output_dir):
    """Create the _index.md file with table of contents."""

    index_content = """# SnapDog2 Blueprint - Table of Contents

This directory contains the complete technical blueprint for SnapDog2, a sophisticated multi-zone audio management system. The blueprint has been split into individual chapters for better organization and navigation.

## Chapters

"""

    # Add chapter links
    for chapter in chapters:
        filename = create_chapter_filename(chapter['num'], chapter['title'])
        index_content += f"{chapter['num']}. [{chapter['title']}]({filename})\n"

    index_content += """
## Overview

SnapDog2 is engineered as a sophisticated and comprehensive **multi-zone audio management system**. Its primary function is to serve as a central control plane within a modern smart home environment, specifically designed to orchestrate audio playback across various physically distinct areas or "zones".

The system integrates with:
- **Snapcast server** infrastructure for synchronized, multi-room audio output
- **Music streaming services** via protocols like Subsonic
- **Internet radio stations**
- **Local media files**
- **MQTT** for flexible, topic-based messaging and eventing
- **KNX** for direct integration with building automation systems

## Architecture

The application employs a **modular, service-oriented architecture** using:
- **.NET 9.0** framework with modern C# features
- **CQRS pattern** with MediatR
- **Result pattern** for error handling
- **Dependency injection** with built-in .NET DI container
- **Comprehensive logging** with Serilog
- **OpenTelemetry** for observability

## Getting Started

For implementation details, start with:
1. [Introduction](01-introduction.md) - Core objectives and use cases
2. [Coding Style & Conventions](02-coding-style-conventions.md) - Development standards
3. [System Architecture](03-system-architecture.md) - High-level design overview
4. [Implementation Plan](21-implementation-plan.md) - Step-by-step development phases
"""

    index_path = output_dir / "_index.md"
    with open(index_path, 'w', encoding='utf-8') as f:
        f.write(index_content)

    print(f"Created index file: {index_path}")

if __name__ == "__main__":
    split_blueprint()
