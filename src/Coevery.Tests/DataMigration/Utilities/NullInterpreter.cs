﻿using Coevery.Data.Migration.Interpreters;
using Coevery.Data.Migration.Schema;

namespace Coevery.Tests.DataMigration.Utilities {
    public class NullInterpreter : IDataMigrationInterpreter {

        public void Visit(ISchemaBuilderCommand command) {
        }

        public void Visit(CreateTableCommand command) {
        }

        public void Visit(DropTableCommand command) {
        }

        public void Visit(AlterTableCommand command) {
        }

        public void Visit(SqlStatementCommand command) {
        }

        public void Visit(CreateForeignKeyCommand command) {
        }

        public void Visit(DropForeignKeyCommand command) {
        }
    }
}