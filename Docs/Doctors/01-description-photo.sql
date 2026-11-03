-- =====================================================================
-- Doctores: descripción + foto + hoja de vida (especialidades por doctor)
-- =====================================================================
-- Agrega las columnas `description` (texto libre) y `photo_url` (URL de la
-- foto en Cloudinary) a la tabla `doctors`, y actualiza las funciones que
-- devuelven doctores para exponer esos campos. También agrega:
--   * update_doctor_photo        -> fija/actualiza la foto del doctor.
--   * get_specialities_by_doctor -> especialidades de un doctor (hoja de vida).
--
-- Nota: las funciones get_all_doctors / get_doctor_by_id /
-- get_doctors_by_speciality cambian su tabla de retorno, por eso hay que
-- DROP + CREATE (CREATE OR REPLACE no permite cambiar el tipo de retorno).
--
-- Aplicado en el branch: develop
-- =====================================================================

-- 1. Columnas nuevas (idempotente) ------------------------------------
alter table doctors
    add column if not exists description text,
    add column if not exists photo_url   text;

-- 2. create_doctor (agrega p_description) -----------------------------
drop function if exists create_doctor(character varying, character varying, character varying, integer, character varying, boolean);

create or replace function public.create_doctor(
    p_name             character varying,
    p_lastname         character varying,
    p_document         character varying,
    p_document_type_id integer,
    p_email            character varying,
    p_status           boolean,
    p_description      text default null
)
returns integer
language plpgsql
as $function$
declare
    s_new_doctor int;
begin
    insert into doctors ("name", lastname, document, document_type_id, email, status, description, created_at)
    values (p_name, p_lastname, p_document, p_document_type_id, p_email, p_status, p_description, now())
    returning id into s_new_doctor;

    return s_new_doctor;
end;
$function$;

-- 3. update_doctor (agrega p_description) -----------------------------
drop function if exists update_doctor(integer, character varying, character varying, character varying, integer, character varying, boolean);

create or replace function public.update_doctor(
    p_id               integer,
    p_name             character varying,
    p_lastname         character varying,
    p_document         character varying,
    p_document_type_id integer,
    p_email            character varying,
    p_status           boolean,
    p_description      text default null
)
returns integer
language plpgsql
as $function$
begin
    update doctors d
    set "name"           = p_name,
        lastname         = p_lastname,
        "document"       = p_document,
        document_type_id = p_document_type_id,
        email            = p_email,
        status           = p_status,
        description      = p_description,
        updated_at       = now()
    where d.id = p_id;

    return p_id;
end;
$function$;

-- 4. update_doctor_photo (nueva) --------------------------------------
create or replace function public.update_doctor_photo(
    p_id        integer,
    p_photo_url text
)
returns integer
language plpgsql
as $function$
begin
    update doctors d
    set photo_url  = p_photo_url,
        updated_at = now()
    where d.id = p_id;

    return p_id;
end;
$function$;

-- 5. get_doctor_by_id (agrega description, photourl) ------------------
drop function if exists get_doctor_by_id(integer);

create or replace function public.get_doctor_by_id(p_id integer)
returns table(
    id               integer,
    name             character varying,
    lastname         character varying,
    document         character varying,
    documenttypeid   integer,
    documenttypename character varying,
    email            character varying,
    status           boolean,
    description      text,
    photourl         text
)
language plpgsql
as $function$
begin
    return query
    select
        d.id               as id,
        d."name"           as "name",
        d.lastname         as lastname,
        d."document"       as "document",
        dt.id              as documenttypeid,
        dt."name"          as documenttypename,
        d.email            as email,
        d.status           as status,
        d.description      as description,
        d.photo_url        as photourl
    from doctors d
    inner join document_types dt on d.document_type_id = dt.id
    where d.id = p_id;
end;
$function$;

-- 6. get_all_doctors (agrega description, photourl) -------------------
drop function if exists get_all_doctors(integer, integer, character varying, boolean);

create or replace function public.get_all_doctors(
    p_limit  integer          default null,
    p_offset integer          default null,
    p_search character varying default ''::character varying,
    p_status boolean          default null
)
returns table(
    id               integer,
    name             character varying,
    lastname         character varying,
    document         character varying,
    documenttypeid   integer,
    documenttypename character varying,
    email            character varying,
    status           boolean,
    description      text,
    photourl         text
)
language plpgsql
as $function$
begin
    return query
    select
        d.id, d."name", d.lastname, d."document",
        dt.id  as documenttypeid, dt."name" as documenttypename,
        d.email, d.status,
        d.description, d.photo_url as photourl
    from doctors d
    inner join document_types dt on d.document_type_id = dt.id
    where (
        p_search is null
        or trim(p_search) = ''
        or lower(trim(coalesce(d."name", '') || ' ' || coalesce(d.lastname, ''))) like '%' || lower(trim(p_search)) || '%'
        or lower(d."document") like '%' || lower(trim(p_search)) || '%'
    )
    and (p_status is null or d.status = p_status)
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$function$;

-- 7. get_doctors_by_speciality (agrega status, description, photourl) -
drop function if exists get_doctors_by_speciality(integer, integer, integer, character varying);

create or replace function public.get_doctors_by_speciality(
    p_speciality_id integer,
    p_limit         integer          default null,
    p_offset        integer          default null,
    p_search        character varying default ''::character varying
)
returns table(
    id               integer,
    name             character varying,
    lastname         character varying,
    document         character varying,
    documenttypeid   integer,
    documenttypename character varying,
    email            character varying,
    status           boolean,
    description      text,
    photourl         text
)
language plpgsql
as $function$
begin
    return query
    select
        d.id               as id,
        d."name"           as "name",
        d.lastname         as lastname,
        d."document"       as "document",
        dt.id              as documenttypeid,
        dt."name"          as documenttypename,
        d.email            as email,
        d.status           as status,
        d.description      as description,
        d.photo_url        as photourl
    from doctors d
    inner join document_types dt on d.document_type_id = dt.id
    inner join doctors_has_specialities dhs on d.id = dhs.doctor_id
    inner join specialities s on dhs.speciality_id = s.id
    where (
        p_search is null
        or trim(p_search) = ''
        or (
            lower(
                trim(
                    coalesce(d."name", '') || ' ' || coalesce(d.lastname, '')
                )
            ) like '%' || lower(trim(p_search)) || '%'
            or lower(d."document") like '%' || lower(trim(p_search)) || '%'
        )
    ) and s.id = p_speciality_id
    limit coalesce(p_limit, 10) offset coalesce(p_offset, 0);
end;
$function$;

-- 8. get_specialities_by_doctor (nueva, para la hoja de vida) ---------
create or replace function public.get_specialities_by_doctor(p_doctor_id integer)
returns table(
    id          integer,
    name        character varying,
    description text,
    code        character varying,
    status      boolean
)
language plpgsql
as $function$
begin
    return query
    select
        s.id          as id,
        s."name"      as "name",
        s.description as description,
        s.code        as code,
        s.status      as status
    from specialities s
    inner join doctors_has_specialities dhs on s.id = dhs.speciality_id
    where dhs.doctor_id = p_doctor_id
    order by s."name";
end;
$function$;
