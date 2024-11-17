DROP TABLE IF EXISTS public.images;

CREATE TABLE IF NOT EXISTS public.images (
    id UUID NOT NULL,
    tag VARCHAR(3000) COLLATE pg_catalog."default",
    src BYTEA,  -- Correct data type for binary data
    user_id UUID NOT NULL,
    CONSTRAINT id PRIMARY KEY (id)
);
