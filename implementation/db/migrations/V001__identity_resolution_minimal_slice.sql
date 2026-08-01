create extension if not exists btree_gist;

create schema if not exists identity_resolution;
create schema if not exists integration_outbox;

create table if not exists identity_resolution.parties (
    tenant_id uuid not null,
    party_id uuid not null,
    created_at timestamptz not null,
    created_by text not null,
    correlation_id uuid not null,
    version bigint not null default 1,
    primary key (tenant_id, party_id),
    unique (tenant_id, correlation_id),
    check (version > 0)
);

create table if not exists identity_resolution.idempotency_records (
    tenant_id uuid not null,
    idempotency_key text not null,
    party_id uuid not null,
    created_at timestamptz not null,
    primary key (tenant_id, idempotency_key),
    foreign key (tenant_id, party_id)
        references identity_resolution.parties (tenant_id, party_id)
);

create table if not exists identity_resolution.party_source_links (
    tenant_id uuid not null,
    source_link_id uuid not null,
    party_id uuid not null,
    source_system text not null,
    source_key text not null,
    match_identity_digest text not null,
    effective_from timestamptz not null,
    effective_to timestamptz null,
    status text not null,
    rule_id text not null,
    rule_version_id text not null,
    ruleset_version text not null,
    source_version text not null,
    evidence_reference text not null,
    created_at timestamptz not null,
    created_by text not null,
    correlation_id uuid not null,
    idempotency_key text not null,
    primary key (tenant_id, source_link_id),
    foreign key (tenant_id, party_id)
        references identity_resolution.parties (tenant_id, party_id),
    unique (tenant_id, idempotency_key),
    check (status in ('active', 'rejected', 'superseded', 'inactive')),
    check (effective_to is null or effective_to > effective_from),
    check (match_identity_digest <> '')
);

create unique index if not exists ux_party_source_links_active_scope
on identity_resolution.party_source_links (tenant_id, source_system, source_key)
where status = 'active' and effective_to is null;

create index if not exists ix_party_source_links_match_identity_digest
on identity_resolution.party_source_links (tenant_id, match_identity_digest)
where status = 'active' and effective_to is null;

alter table identity_resolution.party_source_links
    add constraint ex_party_source_links_active_interval_no_overlap
    exclude using gist (
        tenant_id with =,
        source_system with =,
        source_key with =,
        tstzrange(effective_from, coalesce(effective_to, 'infinity'::timestamptz), '[)') with &&
    ) where (status = 'active');

create index if not exists ix_party_source_links_party
on identity_resolution.party_source_links (tenant_id, party_id);

create table if not exists identity_resolution.review_cases (
    tenant_id uuid not null,
    review_case_id uuid not null,
    source_system text not null,
    source_key text not null,
    match_identity_digest text not null,
    reason text not null,
    correlation_id uuid not null,
    created_at timestamptz not null,
    primary key (tenant_id, review_case_id),
    check (match_identity_digest <> ''),
    check (reason <> '')
);

create index if not exists ix_review_cases_tenant_reason
on identity_resolution.review_cases (tenant_id, reason, created_at);

create table if not exists integration_outbox.outbox_facts (
    tenant_id uuid not null,
    outbox_fact_id uuid not null,
    aggregate_type text not null,
    aggregate_id uuid not null,
    event_type text not null,
    event_version text not null,
    payload jsonb not null,
    occurred_at timestamptz not null,
    correlation_id uuid not null,
    causation_id uuid null,
    idempotency_key text not null,
    publish_status text not null default 'pending',
    created_at timestamptz not null,
    primary key (tenant_id, outbox_fact_id),
    unique (tenant_id, idempotency_key, event_type),
    check (event_version <> ''),
    check (publish_status in ('pending', 'published', 'failed'))
);

create index if not exists ix_outbox_facts_pending
on integration_outbox.outbox_facts (tenant_id, created_at, outbox_fact_id)
where publish_status = 'pending';
