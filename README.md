# NewsLens-Demo
Parts of the NewsLens repo for demo purposes. Excluding tests. 

System Architecture

flowchart TB

    %% Column 1 - Fact Harvest
    subgraph FH[FactHarvestWorkflow]
        A1[News Providers]
        A2[Deduplication]
        A3[LLM Extraction Pipeline]
        A1 --> A2 --> A3
    end

    %% Column 2 - Thread Linker
    subgraph TL[ThreadLinkerWorkflow]
        B1[Unassigned Facts]
        B2[Similarity Calculation]
        B3{Above Threshold?}
        B4[Assign to Existing Thread]
        B5[Create New Thread]
        B1 --> B2 --> B3
        B3 -->|Yes| B4
        B3 -->|No| B5
    end

    %% Column 3 - Article Creation
    subgraph AC[ArticleCreationWorkflow]
        C1[Threads Meeting Criteria]
        C2[Fetch Facts and Metadata]
        C3[LLM Article Generation]
        C1 --> C2 --> C3
    end

    %% Shared Database
    DB[(Azure SQL Database)]

    %% Connections to DB
    A3 --> DB
    DB --> B1
    B5 --> DB
    DB --> C1
    C3 --> DB
    B4 --> DB
