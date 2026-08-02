alter table identity_resolution.parties
    add column if not exists status text not null default 'active',
    add column if not exists party_type text not null default 'person',
    add column if not exists merged_into_party_id uuid null;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_parties_status'
          and conrelid = 'identity_resolution.parties'::regclass)
    then
        alter table identity_resolution.parties
            add constraint ck_parties_status
            check (status in ('active', 'merged', 'retired'));
    end if;

    if not exists (
        select 1
        from pg_constraint
        where conname = 'ck_parties_party_type'
          and conrelid = 'identity_resolution.parties'::regclass)
    then
        alter table identity_resolution.parties
            add constraint ck_parties_party_type
            check (party_type in ('person', 'organization'));
    end if;

    if not exists (
        select 1
        from pg_constraint
        where conname = 'fk_parties_merged_into_party'
          and conrelid = 'identity_resolution.parties'::regclass)
    then
        alter table identity_resolution.parties
            add constraint fk_parties_merged_into_party
            foreign key (tenant_id, merged_into_party_id)
            references identity_resolution.parties (tenant_id, party_id);
    end if;
end $$;

create or replace function identity_resolution.current_tenant_id()
returns uuid
language sql
stable
as $$
    select nullif(current_setting('bluto.tenant_id', true), '')::uuid
$$;

alter table identity_resolution.parties enable row level security;
alter table identity_resolution.parties force row level security;
alter table identity_resolution.party_source_links enable row level security;
alter table identity_resolution.party_source_links force row level security;
alter table identity_resolution.review_cases enable row level security;
alter table identity_resolution.review_cases force row level security;
alter table identity_resolution.idempotency_records enable row level security;
alter table identity_resolution.idempotency_records force row level security;

drop policy if exists parties_tenant_isolation on identity_resolution.parties;
create policy parties_tenant_isolation
on identity_resolution.parties
using (tenant_id = identity_resolution.current_tenant_id())
with check (tenant_id = identity_resolution.current_tenant_id());

drop policy if exists party_source_links_tenant_isolation on identity_resolution.party_source_links;
create policy party_source_links_tenant_isolation
on identity_resolution.party_source_links
using (tenant_id = identity_resolution.current_tenant_id())
with check (tenant_id = identity_resolution.current_tenant_id());

drop policy if exists review_cases_tenant_isolation on identity_resolution.review_cases;
create policy review_cases_tenant_isolation
on identity_resolution.review_cases
using (tenant_id = identity_resolution.current_tenant_id())
with check (tenant_id = identity_resolution.current_tenant_id());

drop policy if exists idempotency_records_tenant_isolation on identity_resolution.idempotency_records;
create policy idempotency_records_tenant_isolation
on identity_resolution.idempotency_records
using (tenant_id = identity_resolution.current_tenant_id())
with check (tenant_id = identity_resolution.current_tenant_id());

create or replace function identity_resolution.has_cross_tenant_identity_digest(
    requested_tenant_id uuid,
    requested_match_identity_digest text)
returns boolean
language sql
stable
security definer
set search_path = identity_resolution, pg_temp
as $$
    select exists (
        select 1
        from identity_resolution.party_source_links
        where tenant_id <> requested_tenant_id
          and match_identity_digest = requested_match_identity_digest
          and status = 'active'
          and effective_to is null)
$$;